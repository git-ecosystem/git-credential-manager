using System;
using System.Threading.Tasks;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace GitHub.Tests
{
    public class GitHubAuthenticationTests
    {
        [Fact]
        public async Task GitHubAuthentication_GetAuthenticationAsync_AuthenticationModesNone_ThrowsException()
        {
            var context = new TestCommandContext();
            var auth = new GitHubAuthentication(context);
            await Assert.ThrowsAsync<ArgumentException>("modes",
                () => auth.GetAuthenticationAsync(null, null, AuthenticationModes.None)
            );
        }
    }
}
