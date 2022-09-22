using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitCredentialManager.Authentication;
using GitCredentialManager.Tests.Objects;
using Moq;
using Xunit;

namespace GitCredentialManager.Tests
{
    public class GenericHostProviderTests
    {
        [Theory]
        [InlineData("http", true)]
        [InlineData("HTTP", true)]
        [InlineData("hTtP", true)]
        [InlineData("https", true)]
        [InlineData("HTTPS", true)]
        [InlineData("hTtPs", true)]
        [InlineData("ssh", true)]
        [InlineData("SSH", true)]
        [InlineData("sSh", true)]
        [InlineData("smpt", true)]
        [InlineData("SmtP", true)]
        [InlineData("SMTP", true)]
        [InlineData("imap", true)]
        [InlineData("iMAp", true)]
        [InlineData("IMAP", true)]
        [InlineData("file", true)]
        [InlineData("fIlE", true)]
        [InlineData("FILE", true)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void GenericHostProvider_IsSupported(string protocol, bool expected)
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = protocol,
                ["host"]     = "example.com",
                ["path"]     = "foo/bar",
            });

            var provider = new GenericHostProvider(new TestCommandContext());

            Assert.Equal(expected, provider.IsSupported(input));
        }

        [Fact]
        public void GenericHostProvider_GetCredentialServiceUrl_ReturnsCorrectKey()
        {
            const string expectedService = "https://example.com/foo/bar";

            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "example.com",
                ["path"]     = "foo/bar",
                ["username"] = "john.doe",
            });

            var provider = new GenericHostProvider(new TestCommandContext());

            string actualService = provider.GetServiceName(input);

            Assert.Equal(expectedService, actualService);
        }

        [Fact]
        public async Task GenericHostProvider_CreateCredentialAsync_WiaNotAllowed_ReturnsBasicCredentialNoWiaCheck()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "example.com",
            });

            const string testUserName = "basicUser";
            const string testPassword = "basicPass";
            var basicCredential = new GitCredential(testUserName, testPassword);

            var context = new TestCommandContext
            {
                Settings = {IsWindowsIntegratedAuthenticationEnabled = false}
            };
            var basicAuthMock = new Mock<IBasicAuthentication>();
            basicAuthMock.Setup(x => x.GetCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(basicCredential)
                .Verifiable();
            var wiaAuthMock = new Mock<IWindowsIntegratedAuthentication>();

            var provider = new GenericHostProvider(context, basicAuthMock.Object, wiaAuthMock.Object);

            ICredential credential = await provider.GenerateCredentialAsync(input);

            Assert.NotNull(credential);
            Assert.Equal(testUserName, credential.Account);
            Assert.Equal(testPassword, credential.Password);
            wiaAuthMock.Verify(x => x.GetIsSupportedAsync(It.IsAny<Uri>()), Times.Never);
            basicAuthMock.Verify(x => x.GetCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GenericHostProvider_CreateCredentialAsync_LegacyAuthorityBasic_ReturnsBasicCredentialNoWiaCheck()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "example.com",
            });

            const string testUserName = "basicUser";
            const string testPassword = "basicPass";
            var basicCredential = new GitCredential(testUserName, testPassword);

            var context = new TestCommandContext
            {
                Settings = {LegacyAuthorityOverride = "basic"}
            };
            var basicAuthMock = new Mock<IBasicAuthentication>();
            basicAuthMock.Setup(x => x.GetCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(basicCredential)
                .Verifiable();
            var wiaAuthMock = new Mock<IWindowsIntegratedAuthentication>();

            var provider = new GenericHostProvider(context, basicAuthMock.Object, wiaAuthMock.Object);

            ICredential credential = await provider.GenerateCredentialAsync(input);

            Assert.NotNull(credential);
            Assert.Equal(testUserName, credential.Account);
            Assert.Equal(testPassword, credential.Password);
            wiaAuthMock.Verify(x => x.GetIsSupportedAsync(It.IsAny<Uri>()), Times.Never);
            basicAuthMock.Verify(x => x.GetCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GenericHostProvider_CreateCredentialAsync_NonHttpProtocol_ReturnsBasicCredentialNoWiaCheck()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "smtp",
                ["host"]     = "example.com",
            });

            const string testUserName = "basicUser";
            const string testPassword = "basicPass";
            var basicCredential = new GitCredential(testUserName, testPassword);

            var context = new TestCommandContext();
            var basicAuthMock = new Mock<IBasicAuthentication>();
            basicAuthMock.Setup(x => x.GetCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(basicCredential)
                .Verifiable();
            var wiaAuthMock = new Mock<IWindowsIntegratedAuthentication>();

            var provider = new GenericHostProvider(context, basicAuthMock.Object, wiaAuthMock.Object);

            ICredential credential = await provider.GenerateCredentialAsync(input);

            Assert.NotNull(credential);
            Assert.Equal(testUserName, credential.Account);
            Assert.Equal(testPassword, credential.Password);
            wiaAuthMock.Verify(x => x.GetIsSupportedAsync(It.IsAny<Uri>()), Times.Never);
            basicAuthMock.Verify(x => x.GetCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [PlatformFact(Platforms.Posix)]
        public async Task GenericHostProvider_CreateCredentialAsync_NonWindows_WiaSupported_ReturnsBasicCredential()
        {
            await TestCreateCredentialAsync_ReturnsBasicCredential(wiaSupported: true);
        }

        [PlatformFact(Platforms.Windows)]
        public async Task GenericHostProvider_CreateCredentialAsync_Windows_WiaSupported_ReturnsEmptyCredential()
        {
            await TestCreateCredentialAsync_ReturnsEmptyCredential(wiaSupported: true);
        }

        [Fact]
        public async Task GenericHostProvider_CreateCredentialAsync_WiaNotSupported_ReturnsBasicCredential()
        {
            await TestCreateCredentialAsync_ReturnsBasicCredential(wiaSupported: false);
        }

        #region Helpers

        private static async Task TestCreateCredentialAsync_ReturnsEmptyCredential(bool wiaSupported)
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "example.com",
            });

            var context = new TestCommandContext();
            var basicAuthMock = new Mock<IBasicAuthentication>();
            basicAuthMock.Setup(x => x.GetCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
                         .Verifiable();
            var wiaAuthMock = new Mock<IWindowsIntegratedAuthentication>();
            wiaAuthMock.Setup(x => x.GetIsSupportedAsync(It.IsAny<Uri>()))
                       .ReturnsAsync(wiaSupported);

            var provider = new GenericHostProvider(context, basicAuthMock.Object, wiaAuthMock.Object);

            ICredential credential = await provider.GenerateCredentialAsync(input);

            Assert.NotNull(credential);
            Assert.Equal(string.Empty, credential.Account);
            Assert.Equal(string.Empty, credential.Password);
            basicAuthMock.Verify(x => x.GetCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        private static async Task TestCreateCredentialAsync_ReturnsBasicCredential(bool wiaSupported)
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "example.com",
            });

            const string testUserName = "basicUser";
            const string testPassword = "basicPass";
            var basicCredential = new GitCredential(testUserName, testPassword);

            var context = new TestCommandContext();
            var basicAuthMock = new Mock<IBasicAuthentication>();
            basicAuthMock.Setup(x => x.GetCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
                         .ReturnsAsync(basicCredential)
                         .Verifiable();
            var wiaAuthMock = new Mock<IWindowsIntegratedAuthentication>();
            wiaAuthMock.Setup(x => x.GetIsSupportedAsync(It.IsAny<Uri>()))
                       .ReturnsAsync(wiaSupported);

            var provider = new GenericHostProvider(context, basicAuthMock.Object, wiaAuthMock.Object);

            ICredential credential = await provider.GenerateCredentialAsync(input);

            Assert.NotNull(credential);
            Assert.Equal(testUserName, credential.Account);
            Assert.Equal(testPassword, credential.Password);
            basicAuthMock.Verify(x => x.GetCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        #endregion
    }
}
