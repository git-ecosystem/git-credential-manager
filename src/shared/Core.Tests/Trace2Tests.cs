using System;
using System.Text.RegularExpressions;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace GitCredentialManager.Tests;

public class Trace2Tests
{
    [PlatformTheory(Platforms.Posix)]
    [InlineData("af_unix:foo", "foo")]
    [InlineData("af_unix:stream:foo-bar", "foo-bar")]
    [InlineData("af_unix:dgram:foo-bar-baz", "foo-bar-baz")]
    public void TryParseEventTarget_Posix_Returns_Expected_Value(string input, string expected)
    {
        var environment = new TestEnvironment();
        var settings = new TestSettings();

        var trace2 = new Trace2(environment, settings.GetTrace2Settings(), new []{""}, DateTimeOffset.UtcNow);
        var isSuccessful = trace2.TryGetPipeName(input, out var actual);

        Assert.True(isSuccessful);
        Assert.Matches(actual, expected);
    }

    [PlatformTheory(Platforms.Windows)]
    [InlineData("\\\\.\\pipe\\git-foo", "git-foo")]
    [InlineData("\\\\.\\pipe\\git-foo-bar", "git-foo-bar")]
    [InlineData("\\\\.\\pipe\\foo\\git-bar", "git-bar")]
    public void TryParseEventTarget_Windows_Returns_Expected_Value(string input, string expected)
    {
        var environment = new TestEnvironment();
        var settings = new TestSettings();

        var trace2 = new Trace2(environment, settings.GetTrace2Settings(), new []{""}, DateTimeOffset.UtcNow);
        var isSuccessful = trace2.TryGetPipeName(input, out var actual);

        Assert.True(isSuccessful);
        Assert.Matches(actual, expected);
    }

    [Theory]
    [InlineData("20190408T191610.507018Z-H9b68c35f-P000059a8")]
    [InlineData("")]
    public void SetSid_Envar_Returns_Expected_Value(string parentSid)
    {
        Regex rx = new Regex(@$"{parentSid}\/[\d\w-]*");

        var environment = new TestEnvironment();
        environment.Variables.Add("GIT_TRACE2_PARENT_SID", parentSid);

        var settings = new TestSettings();
        var trace2 = new Trace2(environment, settings.GetTrace2Settings(), new []{""}, DateTimeOffset.UtcNow);
        var sid = trace2.SetSid();

        Assert.Matches(rx, sid);
    }
}
