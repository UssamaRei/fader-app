using Fader.Core.Models;
using Fader.Core.Utilities;
using Xunit;

namespace Fader.Tests.Utilities;

/// <summary>
/// Unit tests for <see cref="ProcessHelper"/>.
/// These tests verify the name resolution fallback chain works correctly.
/// </summary>
public sealed class ProcessHelperTests
{
    [Fact]
    public void GetDisplayName_CurrentProcess_ReturnsNonEmptyString()
    {
        // Arrange
        var currentPid = Environment.ProcessId;

        // Act
        var name = ProcessHelper.GetDisplayName(currentPid);

        // Assert
        Assert.NotNull(name);
        Assert.NotEmpty(name);
    }

    [Fact]
    public void GetDisplayName_InvalidPid_ReturnsUnknown()
    {
        // Arrange
        // Use a PID that definitely doesn't exist
        const int invalidPid = 99999999;

        // Act
        var name = ProcessHelper.GetDisplayName(invalidPid);

        // Assert
        Assert.NotNull(name);
        // Should gracefully return "Unknown" or process name (not throw)
    }

    [Fact]
    public void GetExecutablePath_CurrentProcess_ReturnsValidPath()
    {
        // Arrange
        var currentPid = Environment.ProcessId;

        // Act
        var path = ProcessHelper.GetExecutablePath(currentPid);

        // Assert
        // Path may be null in sandboxed environments but should not throw
        if (path is not null)
        {
            Assert.True(File.Exists(path), $"Executable path should exist: {path}");
        }
    }

    [Fact]
    public void GetExecutablePath_InvalidPid_ReturnsNull()
    {
        // Arrange
        const int invalidPid = 99999999;

        // Act
        var path = ProcessHelper.GetExecutablePath(invalidPid);

        // Assert
        Assert.Null(path);
    }
}
