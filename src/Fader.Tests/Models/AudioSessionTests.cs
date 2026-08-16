using Fader.Core.Models;
using Xunit;

namespace Fader.Tests.Models;

/// <summary>
/// Unit tests for <see cref="AudioSession"/>.
/// </summary>
public sealed class AudioSessionTests
{
    [Fact]
    public void AudioSession_DefaultRole_IsNone()
    {
        var session = new AudioSession();
        Assert.Equal(AppRole.None, session.Role);
    }

    [Fact]
    public void AudioSession_Equality_BasedOnSessionId()
    {
        var session1 = new AudioSession { SessionId = "abc-123" };
        var session2 = new AudioSession { SessionId = "abc-123" };
        var session3 = new AudioSession { SessionId = "xyz-789" };

        Assert.Equal(session1, session2);
        Assert.NotEqual(session1, session3);
    }

    [Fact]
    public void AudioSession_ToString_ContainsExpectedFields()
    {
        var session = new AudioSession
        {
            DisplayName = "Spotify",
            ProcessId = 1234,
            Volume = 0.35f,
            IsPlaying = true,
            Role = AppRole.Background
        };

        var str = session.ToString();

        Assert.Contains("Spotify", str);
        Assert.Contains("1234", str);
        Assert.Contains("Background", str);
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    public void AudioSession_Volume_CanBeSetToAnyValidScalar(float volume)
    {
        var session = new AudioSession { Volume = volume };
        Assert.Equal(volume, session.Volume);
    }

    [Theory]
    [InlineData(-0.2f, 0.0f)]
    [InlineData(1.5f, 1.0f)]
    public void AudioSession_Volume_ClampsOutOfRangeValues(float input, float expected)
    {
        var session = new AudioSession { Volume = input };
        Assert.Equal(expected, session.Volume);
    }

    [Fact]
    public void AudioSession_IsMuted_ReturnsTrueWhenZeroOrNegative()
    {
        var session = new AudioSession { Volume = 0.0f };
        Assert.True(session.IsMuted);

        session.Volume = 0.5f;
        Assert.False(session.IsMuted);
    }
}
