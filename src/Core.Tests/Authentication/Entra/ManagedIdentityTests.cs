using GitCredentialManager.Authentication.Entra;
using Xunit;

namespace GitCredentialManager.Tests.Authentication.Entra;

public class ManagedIdentityTests
{
    [Theory]
    [InlineData("system")]
    [InlineData("SYSTEM")]
    [InlineData("sYsTeM")]
    public void TryCreate_SystemId_ReturnsSystemIdentity(string value)
    {
        Assert.True(ManagedIdentity.TryCreate(value, out ManagedIdentity identity));
        Assert.Same(ManagedIdentity.System, identity);
    }

    [Theory]
    [InlineData("8B49DCA0-1298-4A0D-AD6D-934E40230839")]
    [InlineData("id://8B49DCA0-1298-4A0D-AD6D-934E40230839")]
    [InlineData("ID://8B49DCA0-1298-4A0D-AD6D-934E40230839")]
    [InlineData("Id://8B49DCA0-1298-4A0D-AD6D-934E40230839")]
    public void TryCreate_ClientId_ReturnsUserIdentity(string value)
    {
        Assert.True(ManagedIdentity.TryCreate(value, out ManagedIdentity identity));
        Assert.Equal("id://8b49dca0-1298-4a0d-ad6d-934e40230839", identity.Id);
    }

    [Theory]
    [InlineData("resource://8B49DCA0-1298-4A0D-AD6D-934E40230839")]
    [InlineData("RESOURCE://8B49DCA0-1298-4A0D-AD6D-934E40230839")]
    [InlineData("rEsOuRcE://8B49DCA0-1298-4A0D-AD6D-934E40230839")]
    public void TryCreate_ResourceId_ReturnsUserIdentity(string value)
    {
        Assert.True(ManagedIdentity.TryCreate(value, out ManagedIdentity identity));
        Assert.Equal("resource://8b49dca0-1298-4a0d-ad6d-934e40230839", identity.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("unknown://8B49DCA0-1298-4A0D-AD6D-934E40230839")]
    [InlineData("this is a string")]
    public void TryCreate_InvalidId_ReturnsFalse(string value)
    {
        Assert.False(ManagedIdentity.TryCreate(value, out ManagedIdentity identity));
        Assert.Null(identity);
    }
}
