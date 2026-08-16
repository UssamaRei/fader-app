using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Fader.Core.Models;

/// <summary>
/// Represents a single Windows audio session (one per application / audio stream).
/// This is the primary domain model that flows through the entire system.
/// </summary>
public sealed class AudioSession : INotifyPropertyChanged
{
    // ─── Identity ────────────────────────────────────────────────────────────

    /// <summary>
    /// Unique identifier for this audio session.
    /// This is the WASAPI session identifier string, which is stable across restarts
    /// for the same application instance.
    /// </summary>
    public string SessionId { get; init; } = string.Empty;

    /// <summary>The Win32 process ID that owns this audio session.</summary>
    public int ProcessId { get; init; }

    /// <summary>
    /// Display name for the application (e.g. "Spotify", "Google Chrome").
    /// Resolved from the process using <see cref="Utilities.ProcessHelper"/>.
    /// Falls back to the executable name without extension if metadata is unavailable.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Absolute path to the process executable.
    /// Used to extract the application icon.
    /// </summary>
    public string ExecutablePath { get; set; } = string.Empty;

    // ─── Audio State ──────────────────────────────────────────────────────────

    private float _volume;
    /// <summary>
    /// Current master volume scalar of this session, in the range [0.0, 1.0].
    /// Clamped automatically to [0.0, 1.0].
    /// This value reflects the actual Windows volume at the time of last refresh.
    /// </summary>
    public float Volume
    {
        get => _volume;
        set => SetProperty(ref _volume, Utilities.VolumeHelper.ClampVolume(value));
    }

    /// <summary>
    /// Gets whether the current session is effectively muted (volume is zero).
    /// </summary>
    public bool IsMuted => _volume <= Utilities.VolumeHelper.MinVolume;

    private bool _isPlaying;
    /// <summary>
    /// Whether the application is currently producing audio output.
    /// Updated by <see cref="Audio.AudioMonitor"/> via WASAPI session state events.
    /// </summary>
    public bool IsPlaying
    {
        get => _isPlaying;
        set => SetProperty(ref _isPlaying, value);
    }

    // ─── Fader Role ───────────────────────────────────────────────────────────

    private AppRole _role = AppRole.None;
    /// <summary>
    /// The role this application plays in the ducking system.
    /// Persisted to settings by <see cref="Services.SettingsService"/>.
    /// </summary>
    public AppRole Role
    {
        get => _role;
        set => SetProperty(ref _role, value);
    }

    // ─── INotifyPropertyChanged ───────────────────────────────────────────────

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // ─── Equality ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Sessions are considered equal when they share the same WASAPI session identifier.
    /// </summary>
    public override bool Equals(object? obj) =>
        obj is AudioSession other && SessionId == other.SessionId;

    public override int GetHashCode() => SessionId.GetHashCode();

    public override string ToString() =>
        $"{DisplayName} [PID={ProcessId}, Vol={Volume:P0}, Playing={IsPlaying}, Role={Role}]";
}
