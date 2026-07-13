using GitCredentialManager.Authentication.Entra;
using Xunit;

namespace GitCredentialManager.Tests.Authentication.Entra;

public class WorkloadFederationOptionsTests
{
    [Fact]
    public void NullAudience_UsesDefault()
    {
        var options = new WorkloadFederationOptions
        {
            Audience = null,
        };

        Assert.Equal(WorkloadFederationOptions.DefaultAudience, options.Audience);
    }
}
