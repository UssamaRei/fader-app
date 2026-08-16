namespace Fader.Core.Utilities;

/// <summary>
/// Utility methods for audio volume calculations, formatting, and boundary constraints.
/// </summary>
public static class VolumeHelper
{
    /// <summary>Minimum permissible volume scalar (silence).</summary>
    public const float MinVolume = 0.0f;

    /// <summary>Maximum permissible volume scalar (full volume).</summary>
    public const float MaxVolume = 1.0f;

    /// <summary>Minimum decibel level considered audible before clamping to silence.</summary>
    public const float MinDecibels = -60.0f;

    /// <summary>
    /// Clamps a volume scalar to the valid range [0.0, 1.0].
    /// Handles NaN / Infinity gracefully by returning MinVolume.
    /// </summary>
    /// <param name="volume">Linear volume scalar.</param>
    /// <returns>Clamped volume between 0.0 and 1.0.</returns>
    public static float ClampVolume(float volume)
    {
        if (float.IsNaN(volume) || float.IsInfinity(volume) || volume < MinVolume)
            return MinVolume;

        if (volume > MaxVolume)
            return MaxVolume;

        return volume;
    }

    /// <summary>
    /// Converts a normalized linear volume scalar [0.0, 1.0] to a percentage integer [0, 100].
    /// </summary>
    /// <param name="volume">Linear volume scalar.</param>
    /// <returns>Integer percentage from 0 to 100.</returns>
    public static int ToPercentage(float volume)
    {
        var clamped = ClampVolume(volume);
        return (int)Math.Round(clamped * 100f, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Converts an integer percentage [0, 100] to a normalized linear volume scalar [0.0, 1.0].
    /// </summary>
    /// <param name="percentage">Volume percentage.</param>
    /// <returns>Linear volume scalar.</returns>
    public static float FromPercentage(int percentage)
    {
        var clamped = Math.Clamp(percentage, 0, 100);
        return clamped / 100.0f;
    }

    /// <summary>
    /// Converts a linear volume scalar [0.0, 1.0] to decibels (dBFS).
    /// </summary>
    /// <param name="volume">Linear volume scalar.</param>
    /// <returns>Decibels value (e.g. 0 dB at 1.0, -60 dB or below for silence).</returns>
    public static float ToDecibels(float volume)
    {
        var clamped = ClampVolume(volume);
        if (clamped <= 0.0001f)
            return MinDecibels;

        var db = 20.0f * MathF.Log10(clamped);
        return Math.Max(MinDecibels, db);
    }

    /// <summary>
    /// Converts a decibel value (dBFS) to a linear volume scalar [0.0, 1.0].
    /// </summary>
    /// <param name="decibels">Decibel value.</param>
    /// <returns>Linear volume scalar.</returns>
    public static float FromDecibels(float decibels)
    {
        if (decibels <= MinDecibels)
            return MinVolume;

        if (decibels >= 0.0f)
            return MaxVolume;

        return MathF.Pow(10.0f, decibels / 20.0f);
    }

    /// <summary>
    /// Formats a volume scalar into a human-readable percentage string (e.g. "75%").
    /// </summary>
    /// <param name="volume">Linear volume scalar.</param>
    /// <returns>Formatted percentage string.</returns>
    public static string FormatPercentage(float volume) => $"{ToPercentage(volume)}%";
}
