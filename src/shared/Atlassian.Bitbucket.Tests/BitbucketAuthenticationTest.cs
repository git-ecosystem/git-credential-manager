using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager;
using GitCredentialManager.Tests.Objects;
using Moq;
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
            context.Settings.IsGuiPromptsEnabled = false; // Force text prompts
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
            context.Settings.IsGuiPromptsEnabled = false; // Force text prompts
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
        public async Task BitbucketAuthentication_GetCredentialsAsync_AllModes_NoUser_BBCloud_HelperCmdLine()
        {
            var targetUri = new Uri("https://bitbucket.org");

            var command = "/usr/bin/test-helper";
            var args = "";
            var expectedUserName = "jsquire";
            var expectedPassword = "password";
            var resultDict = new Dictionary<string, string>
            {
                ["username"] = expectedUserName,
                ["password"] = expectedPassword
            };

            string expectedArgs = $"prompt --show-basic --show-oauth";

            var context = new TestCommandContext();
            context.SessionManager.IsDesktopSession = true; // Enable OAuth and UI helper selection

            var authMock = new Mock<BitbucketAuthentication>(context) { CallBase = true };
            authMock.Setup(x => x.TryFindHelperCommand(out command, out args))
                .Returns(true);
            authMock.Setup(x => x.InvokeHelperAsync(It.IsAny<string>(), It.IsAny<string>(), null, CancellationToken.None))
                .ReturnsAsync(resultDict);

            BitbucketAuthentication auth = authMock.Object;
            CredentialsPromptResult result = await auth.GetCredentialsAsync(targetUri, null, AuthenticationModes.All);

            Assert.Equal(AuthenticationModes.Basic, result.AuthenticationMode);
            Assert.Equal(result.Credential.Account, expectedUserName);
            Assert.Equal(result.Credential.Password, expectedPassword);

            authMock.Verify(x => x.InvokeHelperAsync(command, expectedArgs, null, CancellationToken.None),
                Times.Once);
        }

        [Fact]
        public async Task BitbucketAuthentication_GetCredentialsAsync_BasicOnly_User_BBCloud_HelperCmdLine()
        {
            var targetUri = new Uri("https://bitbucket.org");

            var command = "/usr/bin/test-helper";
            var args = "";
            var expectedUserName = "jsquire";
            var expectedPassword = "password";
            var resultDict = new Dictionary<string, string>
            {
                ["username"] = expectedUserName,
                ["password"] = expectedPassword
            };

            string expectedArgs = $"prompt --username {expectedUserName} --show-basic";

            var context = new TestCommandContext();
            context.SessionManager.IsDesktopSession = true; // Enable UI helper selection

            var authMock = new Mock<BitbucketAuthentication>(context) { CallBase = true };
            authMock.Setup(x => x.TryFindHelperCommand(out command, out args))
                .Returns(true);
            authMock.Setup(x => x.InvokeHelperAsync(It.IsAny<string>(), It.IsAny<string>(), null, CancellationToken.None))
                .ReturnsAsync(resultDict);

            BitbucketAuthentication auth = authMock.Object;
            CredentialsPromptResult result = await auth.GetCredentialsAsync(targetUri, expectedUserName, AuthenticationModes.Basic);

            Assert.Equal(AuthenticationModes.Basic, result.AuthenticationMode);
            Assert.Equal(result.Credential.Account, expectedUserName);
            Assert.Equal(result.Credential.Password, expectedPassword);

            authMock.Verify(x => x.InvokeHelperAsync(command, expectedArgs, null, CancellationToken.None),
                Times.Once);
        }

        [Fact]
        public async Task BitbucketAuthentication_GetCredentialsAsync_AllModes_NoUser_BBServerDC_HelperCmdLine()
        {
            var targetUri = new Uri("https://example.com/bitbucket");

            var command = "/usr/bin/test-helper";
            var args = "";
            var expectedUserName = "jsquire";
            var expectedPassword = "password";
            var resultDict = new Dictionary<string, string>
            {
                ["username"] = expectedUserName,
                ["password"] = expectedPassword
            };

            string expectedArgs = $"prompt --url {targetUri} --show-basic --show-oauth";

            var context = new TestCommandContext();
            context.SessionManager.IsDesktopSession = true; // Enable OAuth and UI helper selection

            var authMock = new Mock<BitbucketAuthentication>(context) { CallBase = true };
            authMock.Setup(x => x.TryFindHelperCommand(out command, out args))
                .Returns(true);
            authMock.Setup(x => x.InvokeHelperAsync(It.IsAny<string>(), It.IsAny<string>(), null, CancellationToken.None))
                .ReturnsAsync(resultDict);

            BitbucketAuthentication auth = authMock.Object;
            CredentialsPromptResult result = await auth.GetCredentialsAsync(targetUri, null, AuthenticationModes.All);

            Assert.Equal(AuthenticationModes.Basic, result.AuthenticationMode);
            Assert.Equal(result.Credential.Account, expectedUserName);
            Assert.Equal(result.Credential.Password, expectedPassword);

            authMock.Verify(x => x.InvokeHelperAsync(command, expectedArgs, null, CancellationToken.None),
                Times.Once);
        }
    }
}
