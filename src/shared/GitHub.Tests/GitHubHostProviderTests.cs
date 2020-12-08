// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.Authentication.OAuth;
using Microsoft.Git.CredentialManager.Tests.Objects;
using Moq;
using Xunit;

namespace GitHub.Tests
{
    public class GitHubHostProviderTests
    {
        [Theory]
        [InlineData("https://github.com", true)]
        [InlineData("https://gitHUB.CoM", true)]
        [InlineData("https://GITHUB.COM", true)]
        [InlineData("https://foogithub.com", false)]
        [InlineData("https://api.github.com", false)]
        public void GitHubHostProvider_IsGitHubDotCom(string input, bool expected)
        {
            Assert.Equal(expected, GitHubHostProvider.IsGitHubDotCom(new Uri(input)));
        }


        [Theory]
        // We report that we support unencrypted HTTP here so that we can fail and
        // show a helpful error message in the call to `GenerateCredentialAsync` instead.
        [InlineData("http", "github.com", true)]
        [InlineData("http", "gist.github.com", true)]
        [InlineData("ssh", "github.com", false)]
        [InlineData("https", "example.com", false)]

        [InlineData("https", "github.com", true)]
        [InlineData("https", "github.con", false)] // No support of phony similar tld.
        [InlineData("https", "gist.github.con", false)] // No support of phony similar tld.
        [InlineData("https", "foogithub.com", false)] // No support of non github.com domains.
        [InlineData("https", "api.github.com", false)] // No support of github.com subdomains.
        [InlineData("https", "gist.github.com", true)] // Except gists.
        [InlineData("https", "GiST.GitHub.Com", true)]
        [InlineData("https", "GitHub.Com", true)]

        [InlineData("http", "github.my-company-server.com", true)]
        [InlineData("http", "gist.github.my-company-server.com", true)]
        [InlineData("https", "github.my-company-server.com", true)]
        [InlineData("https", "gist.github.my-company-server.com", true)]
        [InlineData("https", "gist.my-company-server.com", false)]
        [InlineData("https", "my-company-server.com", false)]
        [InlineData("https", "github.my.company.server.com", true)]
        [InlineData("https", "foogithub.my-company-server.com", false)]
        [InlineData("https", "api.github.my-company-server.com", false)]
        [InlineData("https", "gist.github.my.company.server.com", true)]
        [InlineData("https", "GitHub.My-Company-Server.Com", true)]
        [InlineData("https", "GiST.GitHub.My-Company-Server.com", true)]
        public void GitHubHostProvider_IsSupported(string protocol, string host, bool expected)
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = protocol,
                ["host"] = host,
            });

            var provider = new GitHubHostProvider(new TestCommandContext());
            Assert.Equal(expected, provider.IsSupported(input));
        }


        [Theory]
        [InlineData("https", "github.com", "https://github.com")]
        [InlineData("https", "GitHub.Com", "https://github.com")]
        [InlineData("https", "gist.github.com", "https://github.com")]
        [InlineData("https", "GiST.GitHub.Com", "https://github.com")]
        [InlineData("https", "github.my-company-server.com", "https://github.my-company-server.com")]
        [InlineData("https", "GitHub.My-Company-Server.Com", "https://github.my-company-server.com")]
        [InlineData("https", "gist.github.my-company-server.com", "https://github.my-company-server.com")]
        [InlineData("https", "GiST.GitHub.My-Company-Server.Com", "https://github.my-company-server.com")]
        [InlineData("https", "github.my.company.server.com", "https://github.my.company.server.com")]
        [InlineData("https", "GitHub.My.Company.Server.Com", "https://github.my.company.server.com")]
        [InlineData("https", "gist.github.my.company.server.com", "https://github.my.company.server.com")]
        [InlineData("https", "GiST.GitHub.My.Company.Server.Com", "https://github.my.company.server.com")]
        public void GitHubHostProvider_GetCredentialServiceUrl(string protocol, string host, string expectedService)
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = protocol,
                ["host"] = host,
            });

            var provider = new GitHubHostProvider(new TestCommandContext());
            Assert.Equal(expectedService, provider.GetServiceName(input));
        }


        [Theory]
        [InlineData("https://example.com", "oauth", AuthenticationModes.OAuth)]
        [InlineData("https://github.com", "NOT-A-REAL-VALUE", GitHubConstants.DotComAuthenticationModes)]
        [InlineData("https://GitHub.Com", "NOT-A-REAL-VALUE", GitHubConstants.DotComAuthenticationModes)]
        [InlineData("https://github.com", "none", GitHubConstants.DotComAuthenticationModes)]
        [InlineData("https://GitHub.Com", "none", GitHubConstants.DotComAuthenticationModes)]
        [InlineData("https://github.com", null, GitHubConstants.DotComAuthenticationModes)]
        [InlineData("https://GitHub.Com", null, GitHubConstants.DotComAuthenticationModes)]
        public async Task GitHubHostProvider_GetSupportedAuthenticationModes(string uriString, string gitHubAuthModes, AuthenticationModes expectedModes)
        {
            var targetUri = new Uri(uriString);

            var context = new TestCommandContext { };
            if (gitHubAuthModes != null)
                context.Environment.Variables.Add(GitHubConstants.EnvironmentVariables.AuthenticationModes, gitHubAuthModes);

            var ghApiMock = new Mock<IGitHubRestApi>(MockBehavior.Strict);
            var ghAuthMock = new Mock<IGitHubAuthentication>(MockBehavior.Strict);

            var provider = new GitHubHostProvider(context, ghApiMock.Object, ghAuthMock.Object);

            AuthenticationModes actualModes = await provider.GetSupportedAuthenticationModesAsync(targetUri);

            Assert.Equal(expectedModes, actualModes);
        }


        [Theory]
        [InlineData("https://example.com", null, "0.1", false, AuthenticationModes.None)]
        [InlineData("https://example.com", null, "0.1", true, AuthenticationModes.Basic)]
        [InlineData("https://example.com", null, "100.0", false, AuthenticationModes.OAuth)]
        [InlineData("https://example.com", null, "100.0", true, AuthenticationModes.Basic | AuthenticationModes.OAuth)]
        public async Task GitHubHostProvider_GetSupportedAuthenticationModes_WithMetadata(string uriString, string gitHubAuthModes,
            string installedVersion, bool verifiablePasswordAuthentication, AuthenticationModes expectedModes)
        {
            var targetUri = new Uri(uriString);

            var context = new TestCommandContext { };
            if (gitHubAuthModes != null)
                context.Environment.Variables.Add(GitHubConstants.EnvironmentVariables.AuthenticationModes, gitHubAuthModes);

            var ghApiMock = new Mock<IGitHubRestApi>(MockBehavior.Strict);
            var ghAuthMock = new Mock<IGitHubAuthentication>(MockBehavior.Strict);

            var metaInfo = new GitHubMetaInfo
            {
                InstalledVersion = installedVersion,
                VerifiablePasswordAuthentication = verifiablePasswordAuthentication
            };
            ghApiMock.Setup(x => x.GetMetaInfoAsync(targetUri)).ReturnsAsync(metaInfo);

            var provider = new GitHubHostProvider(context, ghApiMock.Object, ghAuthMock.Object);

            AuthenticationModes actualModes = await provider.GetSupportedAuthenticationModesAsync(targetUri);

            Assert.Equal(expectedModes, actualModes);
        }



        [Fact]
        public async Task GitHubHostProvider_GenerateCredentialAsync_UnencryptedHttp_ThrowsException()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "http",
                ["host"] = "github.com",
            });

            var context = new TestCommandContext();
            var ghApi = Mock.Of<IGitHubRestApi>();
            var ghAuth = Mock.Of<IGitHubAuthentication>();

            var provider = new GitHubHostProvider(context, ghApi, ghAuth);

            await Assert.ThrowsAsync<Exception>(() => provider.GenerateCredentialAsync(input));
        }

        [Fact]
        public async Task GitHubHostProvider_GenerateCredentialAsync_OAuth_ReturnsCredential()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "github.com",
            });

            var expectedTargetUri = new Uri("https://github.com/");
            IEnumerable<string> expectedOAuthScopes = new[]
            {
                GitHubConstants.OAuthScopes.Repo,
                GitHubConstants.OAuthScopes.Gist,
                GitHubConstants.OAuthScopes.Workflow,
            };

            var expectedUserName = "john.doe";
            var tokenValue = "OAUTH-TOKEN";
            var response = new OAuth2TokenResult(tokenValue, "bearer");

            var context = new TestCommandContext();

            var ghAuthMock = new Mock<IGitHubAuthentication>(MockBehavior.Strict);
            ghAuthMock.Setup(x => x.GetAuthenticationAsync(expectedTargetUri, null, It.IsAny<AuthenticationModes>()))
                      .ReturnsAsync(new AuthenticationPromptResult(AuthenticationModes.OAuth));

            ghAuthMock.Setup(x => x.GetOAuthTokenAsync(expectedTargetUri, It.IsAny<IEnumerable<string>>()))
                      .ReturnsAsync(response);

            var ghApiMock = new Mock<IGitHubRestApi>(MockBehavior.Strict);
            ghApiMock.Setup(x => x.GetUserInfoAsync(expectedTargetUri, tokenValue))
                     .ReturnsAsync(new GitHubUserInfo{Login = expectedUserName});

            var provider = new GitHubHostProvider(context, ghApiMock.Object, ghAuthMock.Object);

            ICredential credential = await provider.GenerateCredentialAsync(input);

            Assert.NotNull(credential);
            Assert.Equal(expectedUserName, credential.Account);
            Assert.Equal(tokenValue, credential.Password);

            ghAuthMock.Verify(
                x => x.GetOAuthTokenAsync(
                    expectedTargetUri, expectedOAuthScopes),
                Times.Once);
        }

        [Fact]
        public async Task GitHubHostProvider_GenerateCredentialAsync_Basic_1FAOnly_ReturnsCredential()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "github.com",
            });

            var expectedTargetUri = new Uri("https://github.com/");
            var expectedUserName = "john.doe";
            var expectedPassword = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]
            IEnumerable<string> expectedPatScopes = new[]
            {
                GitHubConstants.TokenScopes.Gist,
                GitHubConstants.TokenScopes.Repo,
            };

            var patValue = "PERSONAL-ACCESS-TOKEN";
            var response = new AuthenticationResult(GitHubAuthenticationResultType.Success, patValue);

            var context = new TestCommandContext();

            var ghAuthMock = new Mock<IGitHubAuthentication>(MockBehavior.Strict);
            ghAuthMock.Setup(x => x.GetAuthenticationAsync(expectedTargetUri, null, It.IsAny<AuthenticationModes>()))
                      .ReturnsAsync(new AuthenticationPromptResult(
                          AuthenticationModes.Basic, new GitCredential(expectedUserName, expectedPassword)));

            var ghApiMock = new Mock<IGitHubRestApi>(MockBehavior.Strict);
            ghApiMock.Setup(x => x.CreatePersonalAccessTokenAsync(expectedTargetUri, expectedUserName, expectedPassword, null, It.IsAny<IEnumerable<string>>()))
                     .ReturnsAsync(response);
            ghApiMock.Setup(x => x.GetUserInfoAsync(expectedTargetUri, patValue))
                     .ReturnsAsync(new GitHubUserInfo{Login = expectedUserName});

            var provider = new GitHubHostProvider(context, ghApiMock.Object, ghAuthMock.Object);

            ICredential credential = await provider.GenerateCredentialAsync(input);

            Assert.NotNull(credential);
            Assert.Equal(expectedUserName, credential.Account);
            Assert.Equal(patValue, credential.Password);

            ghApiMock.Verify(
                x => x.CreatePersonalAccessTokenAsync(
                    expectedTargetUri, expectedUserName, expectedPassword, null, expectedPatScopes),
                Times.Once);
        }

        [Fact]
        public async Task GitHubHostProvider_GenerateCredentialAsync_Basic_2FARequired_ReturnsCredential()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "github.com",
            });

            var expectedTargetUri = new Uri("https://github.com/");
            var expectedUserName = "john.doe";
            var expectedPassword = "letmein123";  // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]
            var expectedAuthCode = "123456";
            IEnumerable<string> expectedPatScopes = new[]
            {
                GitHubConstants.TokenScopes.Gist,
                GitHubConstants.TokenScopes.Repo,
            };

            var patValue = "PERSONAL-ACCESS-TOKEN";
            var response1 = new AuthenticationResult(GitHubAuthenticationResultType.TwoFactorApp);
            var response2 = new AuthenticationResult(GitHubAuthenticationResultType.Success, patValue);

            var context = new TestCommandContext();

            var ghAuthMock = new Mock<IGitHubAuthentication>(MockBehavior.Strict);
            ghAuthMock.Setup(x => x.GetAuthenticationAsync(expectedTargetUri, null, It.IsAny<AuthenticationModes>()))
                      .ReturnsAsync(new AuthenticationPromptResult(
                          AuthenticationModes.Basic, new GitCredential(expectedUserName, expectedPassword)));
            ghAuthMock.Setup(x => x.GetTwoFactorCodeAsync(expectedTargetUri, false))
                      .ReturnsAsync(expectedAuthCode);

            var ghApiMock = new Mock<IGitHubRestApi>(MockBehavior.Strict);
            ghApiMock.Setup(x => x.CreatePersonalAccessTokenAsync(expectedTargetUri, expectedUserName, expectedPassword, null, It.IsAny<IEnumerable<string>>()))
                        .ReturnsAsync(response1);
            ghApiMock.Setup(x => x.CreatePersonalAccessTokenAsync(expectedTargetUri, expectedUserName, expectedPassword, expectedAuthCode, It.IsAny<IEnumerable<string>>()))
                        .ReturnsAsync(response2);
            ghApiMock.Setup(x => x.GetUserInfoAsync(expectedTargetUri, patValue))
                     .ReturnsAsync(new GitHubUserInfo{Login = expectedUserName});

            var provider = new GitHubHostProvider(context, ghApiMock.Object, ghAuthMock.Object);

            ICredential credential = await provider.GenerateCredentialAsync(input);

            Assert.NotNull(credential);
            Assert.Equal(expectedUserName, credential.Account);
            Assert.Equal(patValue, credential.Password);

            ghApiMock.Verify(
                x => x.CreatePersonalAccessTokenAsync(
                    expectedTargetUri, expectedUserName, expectedPassword, null, expectedPatScopes),
                Times.Once);
            ghApiMock.Verify(
                x => x.CreatePersonalAccessTokenAsync(
                    expectedTargetUri, expectedUserName, expectedPassword, expectedAuthCode, expectedPatScopes),
                Times.Once);
        }
    }
}
