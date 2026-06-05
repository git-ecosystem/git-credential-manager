using Xunit;

namespace GitCredentialManager.Tests;

public class GitCapabilitiesTests
{
    [Fact]
    public void Advertised_IncludesState()
    {
        // GCM advertises support for the state capability so the negotiation
        // handshake is functional end-to-end. Individual providers opt in to
        // emitting state/continue piecemeal.
        Assert.True(Constants.SupportedCapabilities.HasFlag(GitCapabilities.State));
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
    public void ParseName_State_ReturnsStateFlag()
    {
        Assert.Equal(GitCapabilities.State, GitCapabilitiesUtils.ParseName("state"));
        Assert.Equal(GitCapabilities.State, GitCapabilitiesUtils.ParseName("STATE"));
        Assert.Equal(GitCapabilities.State, GitCapabilitiesUtils.ParseName("State"));
    }

    [Fact]
    public void ToProtocolName_None_Throws()
    {
        Assert.Throws<System.ArgumentException>(() => GitCapabilitiesUtils.ToProtocolName(GitCapabilities.None));
    }

    [Fact]
    public void ToProtocolName_State_ReturnsStateString()
    {
        Assert.Equal("state", GitCapabilitiesUtils.ToProtocolName(GitCapabilities.State));
    }

    [Fact]
    public void ToProtocolNames_None_ReturnsEmpty()
    {
        Assert.Empty(GitCapabilitiesUtils.ToProtocolNames(GitCapabilities.None));
    }

    [Fact]
    public void ToProtocolNames_State_ReturnsStateOnly()
    {
        Assert.Equal(new[] { "state" }, GitCapabilitiesUtils.ToProtocolNames(GitCapabilities.State));
    }
}
