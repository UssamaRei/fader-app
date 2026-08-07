using System.Threading;
using System.Threading.Tasks;
using Fader.Core.Audio;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using System.Linq;
using System;

namespace Fader.Tests.Audio;

public class FadeEngineTests
{
    private readonly IAudioSessionManager _mockSessionManager;
    private readonly FadeEngine _fadeEngine;

    public FadeEngineTests()
    {
        _mockSessionManager = Substitute.For<IAudioSessionManager>();
        _fadeEngine = new FadeEngine(_mockSessionManager, NullLogger<FadeEngine>.Instance);
    }

    [Fact]
    public async Task FadeVolumeAsync_InterpolatesVolumeCorrectly()
    {
        // Arrange
        string sessionId = "test-session";
        _mockSessionManager.GetVolume(sessionId).Returns(1.0f);

        // Act
        // Fade from 1.0 to 0.0 over 100ms
        await _fadeEngine.FadeVolumeAsync(sessionId, 0.0f, TimeSpan.FromMilliseconds(100));

        // Assert
        // Given 60fps, 100ms is ~6 steps. We should have seen multiple SetVolume calls.
        var setVolumeCalls = _mockSessionManager.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IAudioSessionManager.SetVolume))
            .ToList();

        Assert.True(setVolumeCalls.Count >= 2, "Expected multiple volume interpolation steps.");

        // The final call must be exactly the target volume
        var finalCallArgs = setVolumeCalls.Last().GetArguments();
        Assert.Equal(sessionId, finalCallArgs[0]);
        Assert.Equal(0.0f, (float)finalCallArgs[1]!);
    }

    [Fact]
    public async Task FadeVolumeAsync_CancelsPreviousFadeOnSameSession()
    {
        // Arrange
        string sessionId = "test-session";
        _mockSessionManager.GetVolume(sessionId).Returns(1.0f);

        // Act
        // Start a long fade (1 second)
        var fade1Task = _fadeEngine.FadeVolumeAsync(sessionId, 0.0f, TimeSpan.FromSeconds(1));
        
        // Wait a tiny bit to let it start
        await Task.Delay(100);
        
        // Start a new fade on the same session, which should cancel the first one
        var fade2Task = _fadeEngine.FadeVolumeAsync(sessionId, 0.5f, TimeSpan.FromMilliseconds(50));

        await Task.WhenAll(fade1Task, fade2Task);

        // Assert
        // The final volume must be 0.5f, not 0.0f, because fade1 was canceled.
        var setVolumeCalls = _mockSessionManager.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IAudioSessionManager.SetVolume))
            .ToList();

        var finalCallArgs = setVolumeCalls.Last().GetArguments();
        Assert.Equal(0.5f, (float)finalCallArgs[1]!);
    }
}
