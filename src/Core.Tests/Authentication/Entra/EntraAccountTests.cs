using GitCredentialManager.Authentication.Entra;
using Xunit;

namespace GitCredentialManager.Tests.Authentication.Entra;

public class EntraAccountTests
{
    [Fact]
    public void Equals_DifferentCase_ReturnsTrue()
    {
        var left = new EntraAccount("HOME-ID", "User@Example.com");
        var right = new EntraAccount("home-id", "user@example.com");

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentHomeId_ReturnsFalse()
    {
        var left = new EntraAccount("home-id-1", "user@example.com");
        var right = new EntraAccount("home-id-2", "user@example.com");

        Assert.NotEqual(left, right);
    }
}
