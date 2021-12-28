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
        [InlineData(AuthenticationModes.Browser, true)]
        [InlineData(AuthenticationModes.Browser, false)]
        [InlineData(AuthenticationModes.Device, true)]
        [InlineData(AuthenticationModes.Device, false)]
        public async Task GitHubAuthentication_GetAuthenticationAsync_SingleChoice_TerminalAndInteractionNotRequired(GitHub.AuthenticationModes modes, bool useHelper)
        {
            var context = new TestCommandContext();
            context.Settings.IsTerminalPromptsEnabled = false;
            context.Settings.IsInteractionAllowed = false;
            context.SessionManager.IsDesktopSession = true; // necessary for browser
            if (useHelper)
            {
                context.FileSystem.Files["/usr/local/bin/GitHub.UI"] = new byte[0];
            }
            var auth = new GitHubAuthentication(context);
            var result = await auth.GetAuthenticationAsync(new Uri("https://github.com"), null, modes);
            Assert.Equal(modes, result.AuthenticationMode);
        }

        [Fact]
        public async Task GitHubAuthentication_GetAuthenticationAsync_NonDesktopSession_RequiresTerminal()
        {
            var context = new TestCommandContext();
            context.FileSystem.Files["/usr/local/bin/GitHub.UI"] = new byte[0];
            context.Settings.IsTerminalPromptsEnabled = false;
            var auth = new GitHubAuthentication(context);
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => auth.GetAuthenticationAsync(null, null, AuthenticationModes.All)
            );
            Assert.Equal("Cannot prompt because terminal prompts have been disabled.", exception.Message);
        }

        [Fact]
        public async Task GitHubAuthentication_GetAuthenticationAsync_DesktopSession_RequiresInteraction()
        {
            var context = new TestCommandContext();
            context.FileSystem.Files["/usr/local/bin/GitHub.UI"] = new byte[0];
            context.SessionManager.IsDesktopSession = true;
            context.Settings.IsInteractionAllowed = false;
            var auth = new GitHubAuthentication(context);
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => auth.GetAuthenticationAsync(new Uri("https://github.com"), null, AuthenticationModes.All)
            );
            Assert.Equal("Cannot prompt because user interactivity has been disabled.", exception.Message);
        }
    }
}
