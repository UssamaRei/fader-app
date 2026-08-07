using Fader.Core.Utilities;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Fader.Core.Audio;

/// <summary>
/// Handles smooth volume interpolation for audio sessions.
/// Uses a high-performance PeriodicTimer to achieve 60fps fades without blocking.
/// Automatically cancels ongoing fades if a new fade is requested for the same session.
/// </summary>
public sealed class FadeEngine
{
    private readonly IAudioSessionManager _audioSessionManager;
    private readonly ILogger<FadeEngine> _logger;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeFades = new();

    public FadeEngine(IAudioSessionManager audioSessionManager, ILogger<FadeEngine> logger)
    {
        _audioSessionManager = audioSessionManager;
        _logger = logger;
    }

    /// <summary>
    /// Fades the volume of a specific session to the target volume over the specified duration.
    /// Calling this while a fade is already in progress for the session will cleanly interrupt it.
    /// </summary>
    public async Task FadeVolumeAsync(string sessionId, float targetVolume, TimeSpan duration)
    {
        // Cancel any existing fade for this session
        if (_activeFades.TryRemove(sessionId, out var existingCts))
        {
            existingCts.Cancel();
            existingCts.Dispose();
        }

        var cts = new CancellationTokenSource();
        _activeFades[sessionId] = cts;

        try
        {
            await RunFadeLoopAsync(sessionId, targetVolume, duration, cts.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Fade for session {SessionId} was canceled.", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during fade for session {SessionId}", sessionId);
        }
        finally
        {
            // Clean up if this exact CTS is still the active one
            if (_activeFades.TryGetValue(sessionId, out var currentCts) && currentCts == cts)
            {
                _activeFades.TryRemove(sessionId, out _);
                cts.Dispose();
            }
        }
    }

    private async Task RunFadeLoopAsync(string sessionId, float targetVolume, TimeSpan duration, CancellationToken token)
    {
        var startVolume = _audioSessionManager.GetVolume(sessionId);
        if (startVolume == null) return;

        float start = startVolume.Value;
        float end = Math.Clamp(targetVolume, 0f, 1f);

        if (Math.Abs(start - end) < 0.001f || duration.TotalMilliseconds <= 0)
        {
            _audioSessionManager.SetVolume(sessionId, end);
            return;
        }

        int fps = 60;
        int intervalMs = 1000 / fps;
        int steps = (int)(duration.TotalMilliseconds / intervalMs);
        if (steps <= 0) steps = 1;

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(intervalMs));

        for (int i = 1; i <= steps; i++)
        {
            token.ThrowIfCancellationRequested();
            
            await timer.WaitForNextTickAsync(token);

            // Ease-out cubic interpolation
            float t = (float)i / steps;
            float easedT = 1 - MathF.Pow(1 - t, 3);
            
            float currentVolume = start + (end - start) * easedT;
            _audioSessionManager.SetVolume(sessionId, currentVolume);
        }

        // Ensure final volume is exactly target
        _audioSessionManager.SetVolume(sessionId, end);
    }
}
