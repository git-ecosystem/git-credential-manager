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
        [Fact]
        public void GenericProvider_IsSupported_Http_ReturnsTrue()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "http",
                ["host"]     = "example.com",
                ["path"]     = "foo/bar",
            });

            var provider = new GenericHostProvider(new TestCommandContext());

            Assert.True(provider.IsSupported(input));
        }

        [Fact]
        public void GenericProvider_IsSupported_Https_ReturnsTrue()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "example.com",
                ["path"]     = "foo/bar",
            });

            var provider = new GenericHostProvider(new TestCommandContext());

            Assert.True(provider.IsSupported(input));
        }

        [Fact]
        public void GenericProvider_IsSupported_NonHttp_ReturnsFalse()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "ssh",
                ["host"]     = "example.com",
                ["path"]     = "foo/bar",
            });

            var provider = new GenericHostProvider(new TestCommandContext());

            Assert.False(provider.IsSupported(input));
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

        [Fact]
        public async Task GenericProvider_CreateCredentialAsync_Ntlm_ReturnsEmptyCredential()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "example.com",
            });

            var context = new TestCommandContext();
            var basicAuthMock = new Mock<IBasicAuthentication>();
            basicAuthMock.Setup(x => x.GetCredentials(It.IsAny<Uri>()))
                         .Verifiable();
            var ntlmAuthMock = new Mock<INtlmAuthentication>();
            ntlmAuthMock.Setup(x => x.IsNtlmSupportedAsync(It.IsAny<Uri>()))
                        .ReturnsAsync(true);

            var provider = new GenericHostProvider(context, basicAuthMock.Object, ntlmAuthMock.Object);

            GitCredential credential = await provider.CreateCredentialAsync(input);

            Assert.NotNull(credential);
            Assert.Equal(string.Empty, credential.UserName);
            Assert.Equal(string.Empty, credential.Password);
            basicAuthMock.Verify(x => x.GetCredentials(It.IsAny<Uri>()), Times.Never);
        }

        [Fact]
        public async Task GenericProvider_CreateCredentialAsync_NonNtlm_ReturnsBasicCredential()
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
            basicAuthMock.Setup(x => x.GetCredentials(It.IsAny<Uri>()))
                         .Returns(basicCredential)
                         .Verifiable();
            var ntlmAuthMock = new Mock<INtlmAuthentication>();
            ntlmAuthMock.Setup(x => x.IsNtlmSupportedAsync(It.IsAny<Uri>()))
                        .ReturnsAsync(false);

            var provider = new GenericHostProvider(context, basicAuthMock.Object, ntlmAuthMock.Object);

            GitCredential credential = await provider.CreateCredentialAsync(input);

            Assert.NotNull(credential);
            Assert.Equal(testUserName, credential.UserName);
            Assert.Equal(testPassword, credential.Password);
            basicAuthMock.Verify(x => x.GetCredentials(It.IsAny<Uri>()), Times.Once);
        }
    }
}
