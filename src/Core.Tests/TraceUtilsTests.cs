using System;
using System.IO;
using System.Text;
using Xunit;

namespace GitCredentialManager.Tests;

public class TraceUtilsTests
{
    [Theory]
    [InlineData("/foo/bar/baz/boo", 10, "...baz/boo")]
    [InlineData("thisfileshouldbetruncated", 12, "...truncated")]
    public void FormatSource_ReturnsExpectedSourceValues(string path, int sourceColumnMaxWidth, string expectedSource)
    {
        string actualSource = TraceUtils.FormatSource(path, sourceColumnMaxWidth);
        Assert.Equal(actualSource, expectedSource);
    }
}