using Xunit;

namespace GitCredentialManager.Tests;

public class GitCapabilitiesTests
{
    [Fact]
    public void Advertised_DefaultsToNone()
    {
        // Until specific capability handling is wired up piecemeal, GCM
        // advertises no capabilities back to Git.
        Assert.Equal(GitCapabilities.None, Constants.SupportedCapabilities);
    }

    [Fact]
    public void ParseName_NullOrWhitespace_ReturnsNone()
    {
        Assert.Equal(GitCapabilities.None, GitCapabilitiesUtils.ParseName(null));
        Assert.Equal(GitCapabilities.None, GitCapabilitiesUtils.ParseName(""));
        Assert.Equal(GitCapabilities.None, GitCapabilitiesUtils.ParseName("   "));
    }

    [Fact]
    public void ParseName_UnknownName_ReturnsNone()
    {
        // Per git-credential(1): "Unrecognised attributes and capabilities are silently discarded."
        Assert.Equal(GitCapabilities.None, GitCapabilitiesUtils.ParseName("totally-unknown"));
    }

    [Fact]
    public void ToProtocolName_None_Throws()
    {
        Assert.Throws<System.ArgumentException>(() => GitCapabilitiesUtils.ToProtocolName(GitCapabilities.None));
    }

    [Fact]
    public void ToProtocolNames_None_ReturnsEmpty()
    {
        Assert.Empty(GitCapabilitiesUtils.ToProtocolNames(GitCapabilities.None));
    }
}
