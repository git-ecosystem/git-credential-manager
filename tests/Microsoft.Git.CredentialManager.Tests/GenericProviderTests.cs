// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager.Authentication;
using Microsoft.Git.CredentialManager.Tests.Objects;
using Moq;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests
{
    public class GenericProviderTests
    {
        [Theory]
        [InlineData("http", true)]
        [InlineData("HTTP", true)]
        [InlineData("hTtP", true)]
        [InlineData("https", true)]
        [InlineData("HTTPS", true)]
        [InlineData("hTtPs", true)]
        [InlineData("ssh", false)]
        [InlineData("SSH", false)]
        [InlineData("sSh", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void GenericProvider_IsSupported(string protocol, bool expected)
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
        public void GenericProvider_GetCredentialKey_ReturnsCorrectKey()
        {
            const string expectedKey = "https://john.doe@example.com/foo/bar";

            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "example.com",
                ["path"]     = "foo/bar",
                ["username"] = "john.doe",
            });

            var provider = new GenericHostProvider(new TestCommandContext());

            string actualKey = provider.GetCredentialKey(input);

            Assert.Equal(expectedKey, actualKey);
        }

        [PlatformFact(Platform.MacOS, Platform.Linux)]
        public async Task GenericProvider_CreateCredentialAsync_NonWindows_WiaSupported_ReturnsBasicCredential()
        {
            await TestCreateCredentialAsync_ReturnsBasicCredential(wiaSupported: true);
        }

        [PlatformFact(Platform.Windows)]
        public async Task GenericProvider_CreateCredentialAsync_Windows_WiaSupported_ReturnsEmptyCredential()
        {
            await TestCreateCredentialAsync_ReturnsEmptyCredential(wiaSupported: true);
        }

        [Fact]
        public async Task GenericProvider_CreateCredentialAsync_WiaNotSupported_ReturnsBasicCredential()
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
            basicAuthMock.Setup(x => x.GetCredentials(It.IsAny<string>(), It.IsAny<string>()))
                         .Verifiable();
            var wiaAuthMock = new Mock<IWindowsIntegratedAuthentication>();
            wiaAuthMock.Setup(x => x.GetIsSupportedAsync(It.IsAny<Uri>()))
                       .ReturnsAsync(wiaSupported);

            var provider = new GenericHostProvider(context, basicAuthMock.Object, wiaAuthMock.Object);

            GitCredential credential = await provider.CreateCredentialAsync(input);

            Assert.NotNull(credential);
            Assert.Equal(string.Empty, credential.UserName);
            Assert.Equal(string.Empty, credential.Password);
            basicAuthMock.Verify(x => x.GetCredentials(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
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
            basicAuthMock.Setup(x => x.GetCredentials(It.IsAny<string>(), It.IsAny<string>()))
                         .Returns(basicCredential)
                         .Verifiable();
            var wiaAuthMock = new Mock<IWindowsIntegratedAuthentication>();
            wiaAuthMock.Setup(x => x.GetIsSupportedAsync(It.IsAny<Uri>()))
                       .ReturnsAsync(wiaSupported);

            var provider = new GenericHostProvider(context, basicAuthMock.Object, wiaAuthMock.Object);

            GitCredential credential = await provider.CreateCredentialAsync(input);

            Assert.NotNull(credential);
            Assert.Equal(testUserName, credential.UserName);
            Assert.Equal(testPassword, credential.Password);
            basicAuthMock.Verify(x => x.GetCredentials(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        #endregion
    }
}
