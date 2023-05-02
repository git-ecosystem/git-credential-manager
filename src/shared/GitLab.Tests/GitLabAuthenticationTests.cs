using System;
using System.Threading.Tasks;
using GitCredentialManager;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace GitLab.Tests
{
    public class GitLabAuthenticationTests
    {
        [Fact]
        public async Task GitLabAuthentication_GetAuthenticationAsync_AuthenticationModesNone_ThrowsException()
        {
            var context = new TestCommandContext();
            var auth = new GitLabAuthentication(context);
            await Assert.ThrowsAsync<ArgumentException>("modes",
                () => auth.GetAuthenticationAsync(null, null, AuthenticationModes.None)
            );
        }

        [Theory]
        [InlineData(AuthenticationModes.Browser)]
        public async Task GitLabAuthentication_GetAuthenticationAsync_SingleChoice_InteractionStillRequired(GitLab.AuthenticationModes modes)
        {
            var context = new TestCommandContext();
            context.Settings.IsInteractionAllowed = true;
            context.SessionManager.IsDesktopSession = true; // necessary for browser
            context.Settings.IsGuiPromptsEnabled = false;
            var auth = new GitLabAuthentication(context);
            var result = await auth.GetAuthenticationAsync(null, null, modes);
            Assert.Equal(modes, result.AuthenticationMode);
        }

        [Fact]
        public async Task GitLabAuthentication_GetAuthenticationAsync_TerminalPromptsDisabled_Throws()
        {
            var context = new TestCommandContext();
            context.Settings.IsTerminalPromptsEnabled = false;
            var auth = new GitLabAuthentication(context);
            var exception = await Assert.ThrowsAsync<Trace2InvalidOperationException>(
                () => auth.GetAuthenticationAsync(null, null, AuthenticationModes.All)
            );
            Assert.Equal("Cannot prompt because terminal prompts have been disabled.", exception.Message);
        }

        [Fact]
        public async Task GitLabAuthentication_GetAuthenticationAsync_Terminal()
        {
            var context = new TestCommandContext();
            var auth = new GitLabAuthentication(context);
            context.SessionManager.IsDesktopSession = true;
            context.Settings.IsGuiPromptsEnabled = false;
            context.Terminal.Prompts["option (enter for default)"] = "";
            var result = await auth.GetAuthenticationAsync(null, null, AuthenticationModes.All);
            Assert.Equal(AuthenticationModes.Browser, result.AuthenticationMode);
        }

        [Fact]
        public async Task GitLabAuthentication_GetAuthenticationAsync_ChoosePat()
        {
            var context = new TestCommandContext();
            var auth = new GitLabAuthentication(context);
            context.Terminal.Prompts["option (enter for default)"] = "";
            context.Terminal.Prompts["Username"] = "username";
            context.Terminal.SecretPrompts["Personal access token"] = "token";
            var result = await auth.GetAuthenticationAsync(null, null, AuthenticationModes.All);
            Assert.Equal(AuthenticationModes.Pat, result.AuthenticationMode);
            Assert.Equal("username", result.Credential.Account);
            Assert.Equal("token", result.Credential.Password);
        }

        [Fact]
        public async Task GitLabAuthentication_GetAuthenticationAsync_ChooseBasic()
        {
            var context = new TestCommandContext();
            var auth = new GitLabAuthentication(context);
            context.Terminal.Prompts["option (enter for default)"] = "2";
            context.Terminal.Prompts["Username"] = "username";
            context.Terminal.SecretPrompts["Password"] = "password";
            var result = await auth.GetAuthenticationAsync(null, null, AuthenticationModes.All);
            Assert.Equal(AuthenticationModes.Basic, result.AuthenticationMode);
            Assert.Equal("username", result.Credential.Account);
            Assert.Equal("password", result.Credential.Password);
        }

        [Fact]
        public async Task GitLabAuthentication_GetAuthenticationAsync_AuthenticationModesAll_RequiresInteraction()
        {
            var context = new TestCommandContext();
            context.Settings.IsInteractionAllowed = false;
            var auth = new GitLabAuthentication(context);
            var exception = await Assert.ThrowsAsync<Trace2InvalidOperationException>(
                () => auth.GetAuthenticationAsync(new Uri("https://GitLab.com"), null, AuthenticationModes.All)
            );
            Assert.Equal("Cannot prompt because user interactivity has been disabled.", exception.Message);
        }
    }
}
