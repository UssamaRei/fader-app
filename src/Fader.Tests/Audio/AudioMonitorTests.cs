using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Fader.Core.Audio;
using Fader.Core.Models;
using Fader.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Fader.Tests.Audio;

public class AudioMonitorTests
{
    private readonly IAudioSessionManager _mockSessionManager;
    private readonly ISettingsService _mockSettingsService;
    private readonly AudioMonitor _audioMonitor;

    public AudioMonitorTests()
    {
        _mockSessionManager = Substitute.For<IAudioSessionManager>();
        _mockSettingsService = Substitute.For<ISettingsService>();
        
        // Use very short times for fast test execution
        _mockSettingsService.GetSettings().Returns(new FaderSettings 
        { 
            MinPlaybackDurationMs = 50, 
            RestoreDelayMs = 50 
        });

        _audioMonitor = new AudioMonitor(_mockSessionManager, _mockSettingsService, NullLogger<AudioMonitor>.Instance);
    }

    [Fact]
    public async Task AudioMonitor_FiresTriggerStarted_AfterDebounce()
    {
        // Arrange
        var session = new AudioSession { SessionId = "1", Role = AppRole.Trigger, IsPlaying = false };
        _mockSessionManager.SessionAdded += Raise.Event<EventHandler<AudioSession>>(_mockSessionManager, session);

        bool triggerFired = false;
        _audioMonitor.TriggerStarted += (s, e) => triggerFired = true;

        // Act
        session.IsPlaying = true; // Triggers PropertyChanged

        // Wait less than debounce
        await Task.Delay(10);
        Assert.False(triggerFired, "Trigger should not fire before debounce elapses.");

        // Wait past debounce
        await Task.Delay(100);

        // Assert
        Assert.True(triggerFired, "Trigger should fire after debounce elapses.");
    }

    [Fact]
    public async Task AudioMonitor_SuppressesTriggerStarted_IfStoppedQuickly()
    {
        // Arrange
        var session = new AudioSession { SessionId = "1", Role = AppRole.Trigger, IsPlaying = false };
        _mockSessionManager.SessionAdded += Raise.Event<EventHandler<AudioSession>>(_mockSessionManager, session);

        bool triggerFired = false;
        _audioMonitor.TriggerStarted += (s, e) => triggerFired = true;

        // Act
        session.IsPlaying = true;
        await Task.Delay(10);
        session.IsPlaying = false; // Stop before debounce elapses

        await Task.Delay(100);

        // Assert
        Assert.False(triggerFired, "Trigger should be suppressed for short bursts.");
    }
}
