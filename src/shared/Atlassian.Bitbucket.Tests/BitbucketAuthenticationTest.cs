using System;
using System.Threading.Tasks;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace Atlassian.Bitbucket.Tests
{
    public class BitbucketAuthenticationTest
    {
        [Theory]
        [InlineData("jsquire", "password")]
        public async Task BitbucketAuthentication_GetCredentialsAsync_Basic_SucceedsAfterUserInput(string username, string password)
        {
            var context = new TestCommandContext();
            context.Terminal.Prompts["Username"] = username;
            context.Terminal.SecretPrompts["Password"] = password;
            Uri targetUri = null;

            var bitbucketAuthentication = new BitbucketAuthentication(context);

            var result = await bitbucketAuthentication.GetCredentialsAsync(targetUri, username, AuthenticationModes.Basic);

            Assert.NotNull(result);
            Assert.Equal(AuthenticationModes.Basic, result.AuthenticationMode);
            Assert.Equal(username, result.Credential.Account);
            Assert.Equal(password, result.Credential.Password);
        }

        [Fact]
        public async Task BitbucketAuthentication_GetCredentialsAsync_OAuth_ReturnsOAuth()
        {
            var context = new TestCommandContext();
            context.SessionManager.IsDesktopSession = true; // Allow OAuth mode
            Uri targetUri = null;

            var bitbucketAuthentication = new BitbucketAuthentication(context);

            var result = await bitbucketAuthentication.GetCredentialsAsync(targetUri, null, AuthenticationModes.OAuth);

            Assert.NotNull(result);
            Assert.Equal(AuthenticationModes.OAuth, result.AuthenticationMode);
            Assert.Null(result.Credential);
        }

        [Fact]
        public async Task BitbucketAuthentication_GetCredentialsAsync_All_ShowsMenu_OAuthOption1()
        {
            var context = new TestCommandContext();
            context.SessionManager.IsDesktopSession = true; // Allow OAuth mode
            context.Terminal.Prompts["option (enter for default)"] = "1";
            Uri targetUri = null;

            var bitbucketAuthentication = new BitbucketAuthentication(context);

            var result = await bitbucketAuthentication.GetCredentialsAsync(targetUri, null, AuthenticationModes.All);

            Assert.NotNull(result);
            Assert.Equal(AuthenticationModes.OAuth, result.AuthenticationMode);
            Assert.Null(result.Credential);
        }

        [Fact]
        public async Task BitbucketAuthentication_GetCredentialsAsync_All_ShowsMenu_BasicOption2()
        {
            const string username = "jsquire";
            const string password = "password";

            var context = new TestCommandContext();
            context.SessionManager.IsDesktopSession = true; // Allow OAuth mode
            context.Terminal.Prompts["option (enter for default)"] = "2";
            context.Terminal.Prompts["Username"] = username;
            context.Terminal.SecretPrompts["Password"] = password;
            Uri targetUri = null;

            var bitbucketAuthentication = new BitbucketAuthentication(context);

            var result = await bitbucketAuthentication.GetCredentialsAsync(targetUri, null, AuthenticationModes.All);

            Assert.NotNull(result);
            Assert.Equal(AuthenticationModes.Basic, result.AuthenticationMode);
            Assert.Equal(username, result.Credential.Account);
            Assert.Equal(password, result.Credential.Password);
        }

        [Fact]
        public async Task BitbucketAuthentication_GetCredentialsAsync_All_NoDesktopSession_BasicOnly()
        {
            const string username = "jsquire";
            const string password = "password";

            var context = new TestCommandContext();
            context.SessionManager.IsDesktopSession = false; // Disallow OAuth mode
            context.Terminal.Prompts["Username"] = username;
            context.Terminal.SecretPrompts["Password"] = password;
            Uri targetUri = null;

            var bitbucketAuthentication = new BitbucketAuthentication(context);

            var result = await bitbucketAuthentication.GetCredentialsAsync(targetUri, null, AuthenticationModes.All);

            Assert.NotNull(result);
            Assert.Equal(AuthenticationModes.Basic, result.AuthenticationMode);
            Assert.Equal(username, result.Credential.Account);
            Assert.Equal(password, result.Credential.Password);
        }

        [Fact]
        public async Task BitbucketAuthentication_ShowOAuthRequiredPromptAsync_SucceedsAfterUserInput()
        {
            var context = new TestCommandContext();
            context.Terminal.Prompts["Press enter to continue..."] = " ";

            var bitbucketAuthentication = new BitbucketAuthentication(context);

            var result = await bitbucketAuthentication.ShowOAuthRequiredPromptAsync();

            Assert.True(result);
            Assert.Equal($"Your account has two-factor authentication enabled.{Environment.NewLine}" +
                                           $"To continue you must complete authentication in your web browser.{Environment.NewLine}", context.Terminal.Messages[0].Item1);
        }
    }
}
