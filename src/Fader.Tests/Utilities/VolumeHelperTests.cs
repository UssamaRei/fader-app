using Fader.Core.Utilities;
using Xunit;

namespace Fader.Tests.Utilities;

/// <summary>
/// Unit tests for <see cref="VolumeHelper"/>.
/// </summary>
public sealed class VolumeHelperTests
{
    [Theory]
    [InlineData(0.0f, 0.0f)]
    [InlineData(0.5f, 0.5f)]
    [InlineData(1.0f, 1.0f)]
    [InlineData(-0.5f, 0.0f)]
    [InlineData(1.5f, 1.0f)]
    [InlineData(float.NaN, 0.0f)]
    [InlineData(float.PositiveInfinity, 0.0f)]
    [InlineData(float.NegativeInfinity, 0.0f)]
    public void ClampVolume_ConstrainsToValidRange(float input, float expected)
    {
        var result = VolumeHelper.ClampVolume(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0.0f, 0)]
    [InlineData(0.5f, 50)]
    [InlineData(0.755f, 76)]
    [InlineData(1.0f, 100)]
    [InlineData(-0.1f, 0)]
    [InlineData(1.2f, 100)]
    public void ToPercentage_CalculatesCorrectPercentage(float volume, int expected)
    {
        var result = VolumeHelper.ToPercentage(volume);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0, 0.0f)]
    [InlineData(50, 0.5f)]
    [InlineData(100, 1.0f)]
    [InlineData(-10, 0.0f)]
    [InlineData(120, 1.0f)]
    public void FromPercentage_ConvertsToCorrectScalar(int percentage, float expected)
    {
        var result = VolumeHelper.FromPercentage(percentage);
        Assert.Equal(expected, result, precision: 2);
    }

    [Fact]
    public void ToDecibels_FullVolume_ReturnsZeroDecibels()
    {
        var db = VolumeHelper.ToDecibels(1.0f);
        Assert.Equal(0.0f, db, precision: 1);
    }

    [Fact]
    public void ToDecibels_Silence_ReturnsMinDecibels()
    {
        var db = VolumeHelper.ToDecibels(0.0f);
        Assert.Equal(VolumeHelper.MinDecibels, db);
    }

    [Fact]
    public void FromDecibels_ZeroDecibels_ReturnsFullVolume()
    {
        var volume = VolumeHelper.FromDecibels(0.0f);
        Assert.Equal(1.0f, volume, precision: 2);
    }

    [Fact]
    public void FromDecibels_MinDecibels_ReturnsZeroVolume()
    {
        var volume = VolumeHelper.FromDecibels(VolumeHelper.MinDecibels);
        Assert.Equal(0.0f, volume);
    }

    [Theory]
    [InlineData(0.0f, "0%")]
    [InlineData(0.42f, "42%")]
    [InlineData(1.0f, "100%")]
    public void FormatPercentage_FormatsExpectedString(float volume, string expected)
    {
        var result = VolumeHelper.FormatPercentage(volume);
        Assert.Equal(expected, result);
    }
}
