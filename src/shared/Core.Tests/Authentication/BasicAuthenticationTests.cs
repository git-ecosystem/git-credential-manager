using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GitCredentialManager.Authentication;
using GitCredentialManager.Tests.Objects;
using Moq;
using Xunit;

namespace GitCredentialManager.Tests.Authentication
{
    public class BasicAuthenticationTests
    {
        [Fact]
        public void BasicAuthentication_GetCredentials_NullResource_ThrowsException()
        {
            var context = new TestCommandContext();
            var basicAuth = new BasicAuthentication(context);

            Assert.ThrowsAsync<ArgumentNullException>(() => basicAuth.GetCredentialsAsync(null));
        }

        [Fact]
        public async Task BasicAuthentication_GetCredentials_NonDesktopSession_ResourceAndUserName_PasswordPromptReturnsCredentials()
        {
            const string testResource = "https://example.com";
            const string testUserName = "john.doe";
            const string testPassword = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]

            var context = new TestCommandContext {SessionManager = {IsDesktopSession = false}};
            context.Terminal.SecretPrompts["Password"] = testPassword; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]

            var basicAuth = new BasicAuthentication(context);

            ICredential credential = await basicAuth.GetCredentialsAsync(testResource, testUserName);

            Assert.Equal(testUserName, credential.Account);
            Assert.Equal(testPassword, credential.Password);
        }

        [Fact]
        public async Task BasicAuthentication_GetCredentials_NonDesktopSession_Resource_UserPassPromptReturnsCredentials()
        {
            const string testResource = "https://example.com";
            const string testUserName = "john.doe";
            const string testPassword = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]

            var context = new TestCommandContext {SessionManager = {IsDesktopSession = false}};
            context.Terminal.Prompts["Username"] = testUserName;
            context.Terminal.SecretPrompts["Password"] = testPassword; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]

            var basicAuth = new BasicAuthentication(context);

            ICredential credential = await basicAuth.GetCredentialsAsync(testResource);

            Assert.Equal(testUserName, credential.Account);
            Assert.Equal(testPassword, credential.Password);
        }

        [Fact]
        public void BasicAuthentication_GetCredentials_NonDesktopSession_NoTerminalPrompts_ThrowsException()
        {
            const string testResource = "https://example.com";

            var context = new TestCommandContext
            {
                SessionManager = {IsDesktopSession = false},
                Settings = {IsInteractionAllowed = false},
            };

            var basicAuth = new BasicAuthentication(context);

            Assert.ThrowsAsync<InvalidOperationException>(() => basicAuth.GetCredentialsAsync(testResource));
        }

        [Fact]
        public async Task BasicAuthentication_GetCredentials_DesktopSession_UIHelper_CallsHelper()
        {
            const string testResource = "https://example.com";
            const string testUserName = "john.doe";
            const string testPassword = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]

            const string unixHelperPath = "/usr/local/bin/git-credential-manager-ui";
            const string windowsHelperPath = @"C:\Program Files\Git Credential Manager\git-credential-manager-ui.exe";
            string helperPath = PlatformUtils.IsWindows() ? windowsHelperPath : unixHelperPath;

            var context = new TestCommandContext
            {
                SessionManager = { IsDesktopSession = true },
                Environment =
                {
                    Variables =
                    {
                        [Constants.EnvironmentVariables.GcmUiHelper] = helperPath
                    }
                }
            };

            context.FileSystem.Files[helperPath] = Array.Empty<byte>();

            var auth = new Mock<BasicAuthentication>(MockBehavior.Strict, context);
            auth.Setup(x => x.InvokeHelperAsync(
                    It.IsAny<string>(),
                    $"basic --resource {testResource}",
                    It.IsAny<StreamReader>(),
                    It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(
                    new Dictionary<string, string>
                    {
                        ["username"] = testUserName,
                        ["password"] = testPassword
                    }
                );

            ICredential credential = await auth.Object.GetCredentialsAsync(testResource);

            Assert.NotNull(credential);
            Assert.Equal(testUserName, credential.Account);
            Assert.Equal(testPassword, credential.Password);
        }

        [Fact]
        public async Task BasicAuthentication_GetCredentials_DesktopSession_UIHelper_UserName_CallsHelper()
        {
            const string testResource = "https://example.com";
            const string testUserName = "john.doe";
            const string testPassword = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]

            const string unixHelperPath = "/usr/local/bin/git-credential-manager-ui";
            const string windowsHelperPath = @"C:\Program Files\Git Credential Manager\git-credential-manager-ui.exe";
            string helperPath = PlatformUtils.IsWindows() ? windowsHelperPath : unixHelperPath;

            var context = new TestCommandContext
            {
                SessionManager = { IsDesktopSession = true },
                Environment =
                {
                    Variables =
                    {
                        [Constants.EnvironmentVariables.GcmUiHelper] = helperPath
                    }
                }
            };

            context.FileSystem.Files[helperPath] = Array.Empty<byte>();

            var auth = new Mock<BasicAuthentication>(MockBehavior.Strict, context);
            auth.Setup(x => x.InvokeHelperAsync(
                    It.IsAny<string>(),
                    $"basic --resource {testResource} --username {testUserName}",
                    It.IsAny<StreamReader>(),
                    It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(
                    new Dictionary<string, string>
                    {
                        ["username"] = testUserName,
                        ["password"] = testPassword
                    }
                );

            ICredential credential = await auth.Object.GetCredentialsAsync(testResource, testUserName);

            Assert.NotNull(credential);
            Assert.Equal(testUserName, credential.Account);
            Assert.Equal(testPassword, credential.Password);
        }
    }
}
