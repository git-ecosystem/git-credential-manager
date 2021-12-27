using System;
using System.Threading.Tasks;
using GitCredentialManager.Tests.Objects;
using Moq;
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

        [Theory]
        [InlineData(AuthenticationModes.Browser)]
        [InlineData(AuthenticationModes.Device)]
        public async Task GitHubAuthentication_GetAuthenticationAsync_TerminalPromptNotRequired(GitHub.AuthenticationModes modes)
        {
            var context = new TestCommandContext();
            context.Settings.IsTerminalPromptsEnabled = false;
            context.SessionManager.IsDesktopSession = true;
            var auth = new GitHubAuthentication(context);
            var result = await auth.GetAuthenticationAsync(null, null, modes);
            Assert.Equal(modes, result.AuthenticationMode);
        }
    }
}
