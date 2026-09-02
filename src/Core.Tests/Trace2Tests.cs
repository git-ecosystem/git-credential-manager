using System;
using Xunit;

namespace GitCredentialManager.Tests;

public class Trace2Tests
{
    [PosixTheory]
    [InlineData("af_unix:foo", "foo")]
    [InlineData("af_unix:foo/bar", "foo/bar")]
    [InlineData("af_unix:stream:foo/bar", "foo/bar")]
    [InlineData("af_unix:dgram:foo/bar/baz", "foo/bar/baz")]
    public void TryGetPipeName_Posix_Returns_Expected_Value(string input, string expected)
    {
        var isSuccessful = Trace2.TryGetPipeName(input, out var actual);

        Assert.True(isSuccessful);
        Assert.Equal(actual, expected);
    }

    [WindowsTheory]
    [InlineData("\\\\.\\pipe\\git-foo", "git-foo")]
    [InlineData("\\\\.\\pipe\\git-foo-bar", "git-foo-bar")]
    [InlineData("\\\\.\\pipe\\foo\\git-bar", "foo\\git-bar")]
    public void TryGetPipeName_Windows_Returns_Expected_Value(string input, string expected)
    {
        var isSuccessful = Trace2.TryGetPipeName(input, out var actual);

        Assert.True(isSuccessful);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("abc", 0)]
    [InlineData("abc/def", 1)]
    [InlineData("abc/def/ghi", 2)]
    [InlineData("abc/", 1)]
    public void GetProcessDepth_ReturnsCorrectDepthh(string sid, int expected)
    {
        int actual = Trace2.GetProcessDepth(sid);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CreateSid_ExistingParentSid_AppendsToExisting()
    {
        var originalSid = Environment.GetEnvironmentVariable(Trace2.SidEnvar);

        try
        {
            // Set parent SID
            const string parentSid = "0ddfc330-30e9-49f3-86d3-6b34d99d51f4";
            Environment.SetEnvironmentVariable(Trace2.SidEnvar, parentSid);

            string actualSid = Trace2.CreateSid();

            const string parentPrefix = $"{parentSid}/";
            Assert.StartsWith(parentPrefix, actualSid);

            string rest = actualSid.Substring(parentPrefix.Length);
            Assert.False(string.IsNullOrWhiteSpace(rest));
        }
        finally
        {
            // Restore original environment variable for this process
            Environment.SetEnvironmentVariable(Trace2.SidEnvar, originalSid);
        }
    }

    [Fact]
    public void CreateSid_NoParentSid_CreatesNew()
    {
        var originalSid = Environment.GetEnvironmentVariable(Trace2.SidEnvar);

        try
        {
            // Clear parent SID
            Environment.SetEnvironmentVariable(Trace2.SidEnvar, null);

            string actualSid = Trace2.CreateSid();

            Assert.False(string.IsNullOrWhiteSpace(actualSid));
            Assert.DoesNotContain("/", actualSid);
        }
        finally
        {
            // Restore original environment variable for this process
            Environment.SetEnvironmentVariable(Trace2.SidEnvar, originalSid);
        }
    }
}
