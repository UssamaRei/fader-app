using System.Collections.Concurrent;
using Fader.Core.Models;
using Microsoft.Extensions.Logging;

namespace Fader.Core.Audio;

/// <summary>
/// Orchestrates the ducking behavior.
/// Listens to AudioMonitor for debounced trigger events, snapshots original volumes,
/// and commands FadeEngine to duck or restore background applications.
/// </summary>
public sealed class DuckingService : IDisposable
{
    private readonly AudioMonitor _audioMonitor;
    private readonly IAudioSessionManager _sessionManager;
    private readonly FadeEngine _fadeEngine;
    private readonly Services.ISettingsService _settingsService;
    private readonly ILogger<DuckingService> _logger;

    private readonly HashSet<string> _activeTriggers = new();
    private readonly ConcurrentDictionary<string, float> _originalVolumes = new();
    
    private bool _isDucked;
    private readonly object _lock = new();

    public DuckingService(
        AudioMonitor audioMonitor, 
        IAudioSessionManager sessionManager, 
        FadeEngine fadeEngine,
        Services.ISettingsService settingsService,
        ILogger<DuckingService> logger)
    {
        _audioMonitor = audioMonitor;
        _sessionManager = sessionManager;
        _fadeEngine = fadeEngine;
        _settingsService = settingsService;
        _logger = logger;

        _audioMonitor.TriggerStarted += OnTriggerStarted;
        _audioMonitor.TriggerStopped += OnTriggerStopped;
        
        // Listen to sessions being added/removed to apply ducking immediately 
        // to newly launched background apps if we are already ducked.
        _sessionManager.SessionAdded += OnSessionAdded;
        _sessionManager.SessionRemoved += OnSessionRemoved;
    }

    private void OnTriggerStarted(object? sender, AudioSession session)
    {
        lock (_lock)
        {
            _activeTriggers.Add(session.SessionId);
            if (!_isDucked && _activeTriggers.Count > 0)
            {
                ApplyDucking();
            }
        }
    }

    private void OnTriggerStopped(object? sender, AudioSession session)
    {
        lock (_lock)
        {
            _activeTriggers.Remove(session.SessionId);
            if (_isDucked && _activeTriggers.Count == 0)
            {
                RestoreVolumes();
            }
        }
    }

    private void OnSessionAdded(object? sender, AudioSession session)
    {
        lock (_lock)
        {
            if (_isDucked && session.Role == AppRole.Background)
            {
                // It's a new background app and we are currently ducked.
                // Snapshot its volume and duck it immediately.
                _originalVolumes[session.SessionId] = session.Volume;
                var settings = _settingsService.GetSettings();
                _ = _fadeEngine.FadeVolumeAsync(session.SessionId, settings.DuckVolume, TimeSpan.FromMilliseconds(settings.FadeDurationMs));
            }
        }
    }

    private void OnSessionRemoved(object? sender, AudioSession session)
    {
        lock (_lock)
        {
            _activeTriggers.Remove(session.SessionId);
            _originalVolumes.TryRemove(session.SessionId, out _);

            // If the last trigger app crashed or closed, restore volumes
            if (_isDucked && _activeTriggers.Count == 0)
            {
                RestoreVolumes();
            }
        }
    }

    private void ApplyDucking()
    {
        _isDucked = true;
        var settings = _settingsService.GetSettings();
        
        if (!settings.DuckingEnabled) return;

        var backgroundSessions = _sessionManager.GetSessions().Where(s => s.Role == AppRole.Background).ToList();
        
        foreach (var session in backgroundSessions)
        {
            // Only snapshot if we don't already have one
            _originalVolumes.TryAdd(session.SessionId, session.Volume);
            
            _ = _fadeEngine.FadeVolumeAsync(session.SessionId, settings.DuckVolume, TimeSpan.FromMilliseconds(settings.FadeDurationMs));
        }
        
        _logger.LogInformation("Applied ducking to {Count} background apps", backgroundSessions.Count);
    }

    private void RestoreVolumes()
    {
        _isDucked = false;
        var settings = _settingsService.GetSettings();

        foreach (var (sessionId, originalVolume) in _originalVolumes)
        {
            _ = _fadeEngine.FadeVolumeAsync(sessionId, originalVolume, TimeSpan.FromMilliseconds(settings.FadeDurationMs));
        }
        
        _originalVolumes.Clear();
        _logger.LogInformation("Restored volumes for background apps");
    }

    public void Dispose()
    {
        _audioMonitor.TriggerStarted -= OnTriggerStarted;
        _audioMonitor.TriggerStopped -= OnTriggerStopped;
        _sessionManager.SessionAdded -= OnSessionAdded;
        _sessionManager.SessionRemoved -= OnSessionRemoved;
    }
}
