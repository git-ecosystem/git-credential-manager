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
}
