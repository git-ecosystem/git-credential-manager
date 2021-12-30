using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GitCredentialManager.Tests.Objects;
using Moq;
using Moq.Protected;
using Xunit;

namespace GitLab.Tests
{
    public class GitLabAuthenticationTests
    {
        [Fact]
        public void GitLabAuthentication_GetAuthenticationAsync_AuthenticationModesNone_ThrowsException()
        {
            var context = new TestCommandContext();
            var auth = new GitLabAuthentication(context);
            Assert.Throws<ArgumentException>("modes",
                () => auth.GetAuthentication(null, null, AuthenticationModes.None)
            );
        }

        [Theory]
        [InlineData(AuthenticationModes.Browser)]
        public void GitLabAuthentication_GetAuthenticationAsync_SingleChoice_TerminalAndInteractionNotRequired(GitLab.AuthenticationModes modes)
        {
            var context = new TestCommandContext();
            context.Settings.IsTerminalPromptsEnabled = false;
            context.Settings.IsInteractionAllowed = false;
            context.SessionManager.IsDesktopSession = true; // necessary for browser
            var auth = new GitLabAuthentication(context);
            var result = auth.GetAuthentication(null, null, modes);
            Assert.Equal(modes, result.AuthenticationMode);
        }

        [Fact]
        public void GitLabAuthentication_GetAuthenticationAsync_TerminalPromptsDisabled_Throws()
        {
            var context = new TestCommandContext();
            context.Settings.IsTerminalPromptsEnabled = false;
            var auth = new GitLabAuthentication(context);
            var exception = Assert.Throws<InvalidOperationException>(
                () => auth.GetAuthentication(null, null, AuthenticationModes.All)
            );
            Assert.Equal("Cannot prompt because terminal prompts have been disabled.", exception.Message);
        }

        [Fact]
        public void GitLabAuthentication_GetAuthenticationAsync_Terminal()
        {
            var context = new TestCommandContext();
            var auth = new GitLabAuthentication(context);
            context.SessionManager.IsDesktopSession = true;
            context.Terminal.Prompts["option (enter for default)"] = "";
            var result = auth.GetAuthentication(null, null, AuthenticationModes.All);
            Assert.Equal(AuthenticationModes.Browser, result.AuthenticationMode);
        }

        [Fact]
        public void GitLabAuthentication_GetAuthenticationAsync_AuthenticationModesAll_RequiresInteraction()
        {
            var context = new TestCommandContext();
            context.Settings.IsInteractionAllowed = false;
            var auth = new GitLabAuthentication(context);
            var exception = Assert.Throws<InvalidOperationException>(
                () => auth.GetAuthentication(new Uri("https://GitLab.com"), null, AuthenticationModes.All)
            );
            Assert.Equal("Cannot prompt because user interactivity has been disabled.", exception.Message);
        }
    }
}