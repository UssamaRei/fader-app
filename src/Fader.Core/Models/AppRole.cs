namespace Fader.Core.Models;

/// <summary>
/// The role an application plays in the ducking system.
/// Designed to be extensible for future profile/mode features.
/// </summary>
public enum AppRole
{
    /// <summary>Not assigned — application is monitored but takes no part in ducking.</summary>
    None = 0,

    /// <summary>
    /// Background app — volume is lowered when a Trigger app is active.
    /// e.g. Spotify, Apple Music, Winamp
    /// </summary>
    Background = 1,

    /// <summary>
    /// Trigger app — when this app produces audio, Background apps are ducked.
    /// e.g. Chrome, Discord, VLC, Teams
    /// </summary>
    Trigger = 2
}
