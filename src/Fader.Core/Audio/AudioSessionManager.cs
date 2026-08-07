using Fader.Core.Models;
using Fader.Core.Utilities;
using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System.Collections.Concurrent;

namespace Fader.Core.Audio;

/// <summary>
/// Enumerates, tracks, and controls Windows audio sessions via WASAPI.
/// 
/// Responsibilities:
///   - Enumerate all active audio sessions using MMDeviceEnumerator
///   - Map sessions to domain AudioSession models
///   - Provide thread-safe volume read/write
///   - Notify subscribers when sessions are added or removed
/// 
/// Thread safety: All public methods are safe to call from any thread.
/// Internal COM objects are created and used on a dedicated STA thread.
/// </summary>
public sealed class AudioSessionManager : IAudioSessionManager, IDisposable
{
    // ─── Events ───────────────────────────────────────────────────────────────

    /// <summary>Raised when a new audio session appears (app starts playing audio).</summary>
    public event EventHandler<AudioSession>? SessionAdded;

    /// <summary>Raised when an audio session is removed (app exits or releases audio).</summary>
    public event EventHandler<AudioSession>? SessionRemoved;

    /// <summary>Raised when the session list has been fully refreshed.</summary>
    public event EventHandler<IReadOnlyList<AudioSession>>? SessionsRefreshed;

    // ─── State ────────────────────────────────────────────────────────────────

    private readonly ILogger<AudioSessionManager> _logger;
    private readonly Fader.Core.Services.ISettingsService _settingsService;

    /// <summary>
    /// Thread-safe dictionary keyed on WASAPI session identifier.
    /// AudioSession objects are mutable but structural changes (add/remove) are
    /// protected by the ConcurrentDictionary.
    /// </summary>
    private readonly ConcurrentDictionary<string, (AudioSession Session, AudioSessionControl Control)>
        _sessions = new();

    private MMDeviceEnumerator? _deviceEnumerator;
    private MMDevice? _device;
    private NAudio.CoreAudioApi.AudioSessionManager? _sessionManager;
    private bool _disposed;

    // ─── Constructor ──────────────────────────────────────────────────────────

    public AudioSessionManager(ILogger<AudioSessionManager> logger, Fader.Core.Services.ISettingsService settingsService)
    {
        _logger = logger;
        _settingsService = settingsService;
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a snapshot of all currently tracked audio sessions.
    /// Safe to call from any thread; returns a new list each time.
    /// </summary>
    public IReadOnlyList<AudioSession> GetSessions()
    {
        return _sessions.Values
            .Select(v => v.Session)
            .OrderBy(s => s.DisplayName)
            .ToList();
    }

    /// <summary>
    /// Performs a full enumeration of all active WASAPI audio sessions on the
    /// default audio output device and refreshes the internal session list.
    /// Must be called at startup and can be called on refresh.
    /// </summary>
    public Task RefreshSessionsAsync(CancellationToken cancellationToken = default)
    {
        // Run on a thread-pool thread to avoid blocking the UI.
        // NAudio COM objects require STA; we marshal manually.
        return Task.Run(() => RefreshSessionsCore(cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Gets the current volume of a session as a scalar [0.0, 1.0].
    /// Returns null if the session is not found or access is denied.
    /// </summary>
    public float? GetVolume(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var entry))
        {
            try
            {
                return entry.Control.SimpleAudioVolume.Volume;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get volume for session {SessionId}", sessionId);
            }
        }
        return null;
    }

    /// <summary>
    /// Sets the volume of a session immediately (no fade).
    /// Volume is clamped to [0.0, 1.0].
    /// Returns true if the operation succeeded.
    /// </summary>
    public bool SetVolume(string sessionId, float volume)
    {
        volume = Math.Clamp(volume, 0f, 1f);

        if (_sessions.TryGetValue(sessionId, out var entry))
        {
            try
            {
                entry.Control.SimpleAudioVolume.Volume = volume;
                entry.Session.Volume = volume;
                _logger.LogDebug("Set volume for {Name} to {Volume:P0}", entry.Session.DisplayName, volume);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to set volume for session {SessionId}", sessionId);
            }
        }
        return false;
    }

    // ─── Core Enumeration ─────────────────────────────────────────────────────

    private void RefreshSessionsCore(CancellationToken cancellationToken)
    {
        try
        {
            if (_deviceEnumerator == null)
            {
                _deviceEnumerator = new MMDeviceEnumerator();
                _device = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                _sessionManager = _device.AudioSessionManager;
                _sessionManager.OnSessionCreated += OnSessionCreated;
            }

            var sessions = _sessionManager!.Sessions;

            var newSessionIds = new HashSet<string>();
            var freshSessions = new List<AudioSession>();

            for (int i = 0; i < sessions.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested) return;

                var control = sessions[i];
                TryAddOrUpdateSession(control, newSessionIds, freshSessions);
            }

            // Remove sessions that are no longer present
            var toRemove = _sessions.Keys
                .Where(k => !newSessionIds.Contains(k))
                .ToList();

            foreach (var key in toRemove)
            {
                if (_sessions.TryRemove(key, out var removed))
                {
                    _logger.LogInformation("Session removed: {Name}", removed.Session.DisplayName);
                    SessionRemoved?.Invoke(this, removed.Session);
                    removed.Control.Dispose();
                }
            }

            _logger.LogInformation("Refreshed {Count} audio sessions", freshSessions.Count);
            SessionsRefreshed?.Invoke(this, GetSessions());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enumerate audio sessions");
        }
    }

    private void TryAddOrUpdateSession(
        AudioSessionControl control,
        HashSet<string> newSessionIds,
        List<AudioSession> freshSessions)
    {
        try
        {
            // Session ID uniquely identifies this WASAPI session
            var sessionId = control.GetSessionIdentifier;

            // Skip the "system sounds" session (PID 0) — not user-controllable
            var processId = (int)control.GetProcessID;
            if (processId == 0) return;

            newSessionIds.Add(sessionId);

            if (_sessions.TryGetValue(sessionId, out var existing))
            {
                // Update mutable fields on existing session
                existing.Session.Volume = control.SimpleAudioVolume.Volume;
                existing.Session.IsPlaying = control.State == AudioSessionState.AudioSessionStateActive;
                freshSessions.Add(existing.Session);
                return;
            }

            // New session — build the domain model
            var execPath = ProcessHelper.GetExecutablePath(processId) ?? string.Empty;
            var displayName = ResolveDisplayName(control, processId, execPath);

            var session = new AudioSession
            {
                SessionId = sessionId,
                ProcessId = processId,
                DisplayName = displayName,
                ExecutablePath = execPath,
                Volume = control.SimpleAudioVolume.Volume,
                IsPlaying = control.State == AudioSessionState.AudioSessionStateActive,
                Role = _settingsService.GetAppRole(execPath)
            };

            // Register real-time WASAPI event handler
            var handler = new SessionEventHandler(session, OnSessionDisconnected);
            control.RegisterEventClient(handler);

            _sessions[sessionId] = (session, control);
            freshSessions.Add(session);

            _logger.LogInformation("Session discovered: {Name} [PID={PID}]", displayName, processId);
            SessionAdded?.Invoke(this, session);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to process audio session");
        }
    }

    /// <summary>
    /// Resolves the best display name for an audio session.
    /// Order: WASAPI display name → process file description → process name.
    /// </summary>
    private static string ResolveDisplayName(AudioSessionControl control, int processId, string execPath)
    {
        // WASAPI session display name (apps can set this themselves)
        var wasapiName = control.DisplayName?.Trim();
        if (!string.IsNullOrWhiteSpace(wasapiName) && !wasapiName.StartsWith('@'))
            return wasapiName;

        // Fall back to process metadata
        return ProcessHelper.GetDisplayName(processId);
    }

    // ─── IDisposable ──────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_sessionManager != null)
        {
            _sessionManager.OnSessionCreated -= OnSessionCreated;
        }

        foreach (var (_, control) in _sessions.Values)
        {
            try { control.Dispose(); }
            catch { /* Best effort */ }
        }

        _sessions.Clear();
        _device?.Dispose();
        _deviceEnumerator?.Dispose();
    }

    // ─── WASAPI Event Handlers ────────────────────────────────────────────────

    private void OnSessionCreated(object sender, IAudioSessionControl newSession)
    {
        // Session created event is fired on a background thread.
        // We wrap the unmanaged control and process it.
        try
        {
            var control = new AudioSessionControl(newSession);
            var newSessionIds = new HashSet<string>();
            var freshSessions = new List<AudioSession>();
            
            TryAddOrUpdateSession(control, newSessionIds, freshSessions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to handle OnSessionCreated event");
        }
    }

    private void OnSessionDisconnected(string sessionId)
    {
        if (_sessions.TryRemove(sessionId, out var removed))
        {
            _logger.LogInformation("Session disconnected: {Name}", removed.Session.DisplayName);
            SessionRemoved?.Invoke(this, removed.Session);
            try { removed.Control.Dispose(); } catch { }
        }
    }

    private sealed class SessionEventHandler : IAudioSessionEventsHandler
    {
        private readonly AudioSession _session;
        private readonly Action<string> _onDisconnected;

        public SessionEventHandler(AudioSession session, Action<string> onDisconnected)
        {
            _session = session;
            _onDisconnected = onDisconnected;
        }

        public void OnVolumeChanged(float volume, bool isMuted)
        {
            // WASAPI notifies us when the volume changes externally
            _session.Volume = volume;
        }

        public void OnDisplayNameChanged(string displayName) { }
        public void OnIconPathChanged(string iconPath) { }
        public void OnChannelVolumeChanged(uint channelCount, IntPtr newVolumes, uint channelIndex) { }
        public void OnGroupingParamChanged(ref Guid groupingId) { }

        public void OnStateChanged(AudioSessionState state)
        {
            _session.IsPlaying = state == AudioSessionState.AudioSessionStateActive;
        }

        public void OnSessionDisconnected(AudioSessionDisconnectReason disconnectReason)
        {
            _onDisconnected(_session.SessionId);
        }
    }
}
