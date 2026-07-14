using System;
using System.Threading.Tasks;
using GitCredentialManager.Authentication.Entra;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace GitCredentialManager.Tests.Authentication.Entra;

public class ServicePrincipalTests
{
    [Fact]
    public async Task GetToken_NoCredential_ThrowsException()
    {
        var context = new TestCommandContext();
        var entraAuth = new EntraAuthentication(context);
        var servicePrincipal = new ServicePrincipalIdentity
        {
            Id = "11111111-1111-1111-1111-111111111111",
            TenantId = "22222222-2222-2222-2222-222222222222",
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => entraAuth.GetTokenForServicePrincipalAsync(["scope"], servicePrincipal));
    }
}
