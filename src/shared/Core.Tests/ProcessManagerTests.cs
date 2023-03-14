using GitCredentialManager;
using Xunit;

namespace Core.Tests;

public class ProcessManagerTests
{
    [Theory]
    [InlineData("", 0)]
    [InlineData("foo", 0)]
    [InlineData("foo/bar", 1)]
    [InlineData("foo/bar/baz", 2)]
    public void CreateSid_Envar_Returns_Expected_Sid(string input, int expected)
    {
        ProcessManager.Sid = input;
        var actual = ProcessManager.GetProcessDepth();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("foo", 0)]
    [InlineData("foo/bar", 1)]
    [InlineData("foo/bar/baz", 2)]
    public void TryGetProcessDepth_Returns_Expected_Depth(string input, int expected)
    {
        ProcessManager.Sid = input;
        var actual = ProcessManager.GetProcessDepth();

        Assert.Equal(expected, actual);
    }
}