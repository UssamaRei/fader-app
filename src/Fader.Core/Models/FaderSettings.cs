namespace Fader.Core.Models;

/// <summary>
/// Application-wide settings model.
/// Serialized to JSON by <see cref="Services.SettingsService"/>.
/// All new settings must have sensible defaults so that missing fields
/// in older settings files are handled gracefully.
/// </summary>
public sealed class FaderSettings
{
    // ─── Ducking Behaviour ────────────────────────────────────────────────────

    /// <summary>
    /// Volume level Background apps are ducked to, as a scalar [0.0, 1.0].
    /// Default: 20% (0.20)
    /// </summary>
    public float DuckVolume { get; set; } = 0.20f;

    /// <summary>
    /// Duration of the fade-down and fade-up animations in milliseconds.
    /// Default: 350ms
    /// </summary>
    public int FadeDurationMs { get; set; } = 350;

    /// <summary>
    /// How long (ms) to wait after a Trigger app stops playing before restoring volume.
    /// Prevents rapid volume bouncing from short audio gaps.
    /// Default: 800ms
    /// </summary>
    public int RestoreDelayMs { get; set; } = 800;

    /// <summary>
    /// Minimum duration (ms) a Trigger app must continuously play before ducking is applied.
    /// Prevents notification sounds from triggering a duck.
    /// Default: 800ms
    /// </summary>
    public int MinPlaybackDurationMs { get; set; } = 800;

    // ─── Application Behaviour ────────────────────────────────────────────────

    /// <summary>Whether ducking is currently enabled.</summary>
    public bool DuckingEnabled { get; set; } = true;

    /// <summary>Whether Fader launches automatically with Windows.</summary>
    public bool LaunchOnStartup { get; set; } = true;

    /// <summary>Whether the main window starts minimized to tray.</summary>
    public bool StartMinimized { get; set; } = false;

    // ─── Per-App Role Assignments ─────────────────────────────────────────────

    /// <summary>
    /// Maps executable name (e.g. "spotify.exe") → AppRole.
    /// Using executable name instead of full path for portability across reinstalls.
    /// </summary>
    public Dictionary<string, AppRole> AppRoles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
