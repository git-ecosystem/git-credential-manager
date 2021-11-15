using System;
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

            Assert.Throws<ArgumentNullException>(() => basicAuth.GetCredentials(null));
        }

        [Fact]
        public void BasicAuthentication_GetCredentials_NonDesktopSession_ResourceAndUserName_PasswordPromptReturnsCredentials()
        {
            const string testResource = "https://example.com";
            const string testUserName = "john.doe";
            const string testPassword = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]

            var context = new TestCommandContext {SessionManager = {IsDesktopSession = false}};
            context.Terminal.SecretPrompts["Password"] = testPassword; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]

            var basicAuth = new BasicAuthentication(context);

            ICredential credential = basicAuth.GetCredentials(testResource, testUserName);

            Assert.Equal(testUserName, credential.Account);
            Assert.Equal(testPassword, credential.Password);
        }

        [Fact]
        public void BasicAuthentication_GetCredentials_NonDesktopSession_Resource_UserPassPromptReturnsCredentials()
        {
            const string testResource = "https://example.com";
            const string testUserName = "john.doe";
            const string testPassword = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]

            var context = new TestCommandContext {SessionManager = {IsDesktopSession = false}};
            context.Terminal.Prompts["Username"] = testUserName;
            context.Terminal.SecretPrompts["Password"] = testPassword; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]

            var basicAuth = new BasicAuthentication(context);

            ICredential credential = basicAuth.GetCredentials(testResource);

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

            Assert.Throws<InvalidOperationException>(() => basicAuth.GetCredentials(testResource));
        }

        [PlatformFact(Platforms.Windows)]
        public void BasicAuthentication_GetCredentials_DesktopSession_Resource_UserPassPromptReturnsCredentials()
        {
            const string testResource = "https://example.com";
            const string testUserName = "john.doe";
            const string testPassword = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]

            var context = new TestCommandContext
            {
                SessionManager = {IsDesktopSession = true},
                SystemPrompts =
                {
                    CredentialPrompt = (resource, userName) =>
                    {
                        Assert.Equal(testResource, resource);
                        Assert.Null(userName);

                        return new GitCredential(testUserName, testPassword);
                    }
                }
            };

            var basicAuth = new BasicAuthentication(context);

            ICredential credential = basicAuth.GetCredentials(testResource);

            Assert.NotNull(credential);
            Assert.Equal(testUserName, credential.Account);
            Assert.Equal(testPassword, credential.Password);
        }

        [PlatformFact(Platforms.Windows)]
        public void BasicAuthentication_GetCredentials_DesktopSession_ResourceAndUser_PassPromptReturnsCredentials()
        {
            const string testResource = "https://example.com";
            const string testUserName = "john.doe";
            const string testPassword = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]

            var context = new TestCommandContext
            {
                SessionManager = {IsDesktopSession = true},
                SystemPrompts =
                {
                    CredentialPrompt = (resource, userName) =>
                    {
                        Assert.Equal(testResource, resource);
                        Assert.Equal(testUserName, userName);

                        return new GitCredential(testUserName, testPassword);
                    }
                }
            };

            var basicAuth = new BasicAuthentication(context);

            ICredential credential = basicAuth.GetCredentials(testResource, testUserName);

            Assert.NotNull(credential);
            Assert.Equal(testUserName, credential.Account);
            Assert.Equal(testPassword, credential.Password);
        }

        [PlatformFact(Platforms.Windows)]
        public void BasicAuthentication_GetCredentials_DesktopSession_ResourceAndUser_PassPromptDiffUserReturnsCredentials()
        {
            const string testResource = "https://example.com";
            const string testUserName = "john.doe";
            const string newUserName  = "jane.doe";
            const string testPassword = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]

            var context = new TestCommandContext
            {
                SessionManager = {IsDesktopSession = true},
                SystemPrompts =
                {
                    CredentialPrompt = (resource, userName) =>
                    {
                        Assert.Equal(testResource, resource);
                        Assert.Equal(testUserName, userName);

                        return new GitCredential(newUserName, testPassword);
                    }
                }
            };

            var basicAuth = new BasicAuthentication(context);

            ICredential credential = basicAuth.GetCredentials(testResource, testUserName);

            Assert.NotNull(credential);
            Assert.Equal(newUserName, credential.Account);
            Assert.Equal(testPassword, credential.Password);
        }
    }
}
