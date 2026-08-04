namespace Fader.Core.Models;

/// <summary>
/// Represents a single Windows audio session (one per application / audio stream).
/// This is the primary domain model that flows through the entire system.
/// </summary>
public sealed class AudioSession
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

    /// <summary>
    /// Current master volume scalar of this session, in the range [0.0, 1.0].
    /// This value reflects the actual Windows volume at the time of last refresh.
    /// </summary>
    public float Volume { get; set; }

    /// <summary>
    /// Whether the application is currently producing audio output.
    /// Updated by <see cref="Audio.AudioMonitor"/> via WASAPI session state events.
    /// </summary>
    public bool IsPlaying { get; set; }

    // ─── Fader Role ───────────────────────────────────────────────────────────

    /// <summary>
    /// The role this application plays in the ducking system.
    /// Persisted to settings by <see cref="Services.SettingsService"/>.
    /// </summary>
    public AppRole Role { get; set; } = AppRole.None;

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
