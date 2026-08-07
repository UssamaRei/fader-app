using Fader.Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Fader.Core.Audio;

/// <summary>
/// Monitors all audio sessions and applies debouncing logic to prevent erratic ducking.
/// Emits stable TriggerStarted and TriggerStopped events.
/// </summary>
public sealed class AudioMonitor : IDisposable
{
    public event EventHandler<AudioSession>? TriggerStarted;
    public event EventHandler<AudioSession>? TriggerStopped;

    private readonly IAudioSessionManager _sessionManager;
    private readonly Services.ISettingsService _settingsService;
    private readonly ILogger<AudioMonitor> _logger;

    // Track active debouncing timers per session
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _debounceTimers = new();

    public AudioMonitor(IAudioSessionManager sessionManager, Services.ISettingsService settingsService, ILogger<AudioMonitor> logger)
    {
        _sessionManager = sessionManager;
        _settingsService = settingsService;
        _logger = logger;

        _sessionManager.SessionAdded += OnSessionAdded;
        _sessionManager.SessionRemoved += OnSessionRemoved;
        _sessionManager.SessionsRefreshed += OnSessionsRefreshed;
    }

    private void OnSessionsRefreshed(object? sender, IReadOnlyList<AudioSession> sessions)
    {
        foreach (var session in sessions)
        {
            SubscribeToSession(session);
        }
    }

    private void OnSessionAdded(object? sender, AudioSession session)
    {
        SubscribeToSession(session);
    }

    private void OnSessionRemoved(object? sender, AudioSession session)
    {
        UnsubscribeFromSession(session);
        CancelDebounceTimer(session.SessionId);
    }

    private void SubscribeToSession(AudioSession session)
    {
        session.PropertyChanged -= OnSessionPropertyChanged;
        session.PropertyChanged += OnSessionPropertyChanged;
        
        // If it's already playing and it's a trigger, kick off the timer immediately
        if (session.IsPlaying && session.Role == AppRole.Trigger)
        {
            RestartDebounceTimer(session, true);
        }
    }

    private void UnsubscribeFromSession(AudioSession session)
    {
        session.PropertyChanged -= OnSessionPropertyChanged;
    }

    private void OnSessionPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is not AudioSession session) return;

        if (e.PropertyName == nameof(AudioSession.IsPlaying) || e.PropertyName == nameof(AudioSession.Role))
        {
            if (session.Role == AppRole.Trigger)
            {
                RestartDebounceTimer(session, session.IsPlaying);
            }
            else
            {
                CancelDebounceTimer(session.SessionId);
            }
        }
    }

    private void CancelDebounceTimer(string sessionId)
    {
        if (_debounceTimers.TryRemove(sessionId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    private void RestartDebounceTimer(AudioSession session, bool isPlaying)
    {
        CancelDebounceTimer(session.SessionId);

        var cts = new CancellationTokenSource();
        _debounceTimers[session.SessionId] = cts;
        
        var settings = _settingsService.GetSettings();
        var delayMs = isPlaying ? settings.MinPlaybackDurationMs : settings.RestoreDelayMs;

        _ = RunDebounceAsync(session, isPlaying, delayMs, cts.Token);
    }

    private async Task RunDebounceAsync(AudioSession session, bool isPlaying, int delayMs, CancellationToken token)
    {
        try
        {
            await Task.Delay(delayMs, token);
            
            // If we made it here without cancellation, the state is stable
            if (isPlaying)
            {
                _logger.LogInformation("Trigger Started: {Name}", session.DisplayName);
                TriggerStarted?.Invoke(this, session);
            }
            else
            {
                _logger.LogInformation("Trigger Stopped: {Name}", session.DisplayName);
                TriggerStopped?.Invoke(this, session);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when state changes before timer elapses
        }
    }

    public void Dispose()
    {
        _sessionManager.SessionAdded -= OnSessionAdded;
        _sessionManager.SessionRemoved -= OnSessionRemoved;
        _sessionManager.SessionsRefreshed -= OnSessionsRefreshed;

        foreach (var cts in _debounceTimers.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }
        _debounceTimers.Clear();
    }
}
