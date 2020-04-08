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

        [Fact]
        public void GitHubHostProvider_IsSupported_GitHubHost_UnencryptedHttp_ReturnsTrue()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "http",
                ["host"] = "github.com",
            });

            var provider = new GitHubHostProvider(new TestCommandContext());

            // We report that we support unencrypted HTTP here so that we can fail and
            // show a helpful error message in the call to `GenerateCredentialAsync` instead.
            Assert.True(provider.IsSupported(input));
        }

        [Fact]
        public void GitHubHostProvider_IsSupported_GistHost_UnencryptedHttp_ReturnsTrue()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "http",
                ["host"] = "gist.github.com",
            });

            var provider = new GitHubHostProvider(new TestCommandContext());

            // We report that we support unencrypted HTTP here so that we can fail and
            // show a helpful error message in the call to `GenerateCredentialAsync` instead.
            Assert.True(provider.IsSupported(input));
        }

        [Fact]
        public void GitHubHostProvider_IsSupported_GitHubHost_Https_ReturnsTrue()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "github.com",
            });

            var provider = new GitHubHostProvider(new TestCommandContext());

            Assert.True(provider.IsSupported(input));
        }

        [Fact]
        public void GitHubHostProvider_IsSupported_GistHost_Https_ReturnsTrue()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "gist.github.com",
            });

            var provider = new GitHubHostProvider(new TestCommandContext());

            Assert.True(provider.IsSupported(input));
        }

        [Fact]
        public void GitHubHostProvider_IsSupported_NonHttpHttps_ReturnsTrue()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "ssh",
                ["host"] = "github.com",
            });

            var provider = new GitHubHostProvider(new TestCommandContext());

            Assert.False(provider.IsSupported(input));
        }

        [Fact]
        public void GitHubHostProvider_IsSupported_NonGitHub_ReturnsFalse()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "example.com",
            });

            var provider = new GitHubHostProvider(new TestCommandContext());
            Assert.False(provider.IsSupported(input));
        }

        [Fact]
        public void GitHubHostProvider_GetCredentialKey_GitHubHost_ReturnsCorrectKey()
        {
            const string expectedKey = "git:https://github.com";
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "github.com",
            });

            var provider = new GitHubHostProvider(new TestCommandContext());
            string actualKey = provider.GetCredentialKey(input);
            Assert.Equal(expectedKey, actualKey);
        }

        [Fact]
        public void GitHubHostProvider_GetCredentialKey_GistHost_ReturnsCorrectKey()
        {
            const string expectedKey = "git:https://github.com";
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "gist.github.com",
            });

            var provider = new GitHubHostProvider(new TestCommandContext());
            string actualKey = provider.GetCredentialKey(input);
            Assert.Equal(expectedKey, actualKey);
        }

        [Fact]
        public async Task GitHubHostProvider_GetSupportedAuthenticationModes_Override_ReturnsOverrideValue()
        {
            var targetUri = new Uri("https://example.com");
            var expectedModes = AuthenticationModes.OAuth;

            var context = new TestCommandContext
            {
                Settings =
                {
                    Environment =
                    {
                        Variables =
                        {
                            [GitHubConstants.EnvironmentVariables.AuthenticationModes] = expectedModes.ToString()
                        }
                    }
                }
            };

            var ghApiMock = new Mock<IGitHubRestApi>(MockBehavior.Strict);
            var ghAuthMock = new Mock<IGitHubAuthentication>(MockBehavior.Strict);

            var provider = new GitHubHostProvider(context, ghApiMock.Object, ghAuthMock.Object);

            AuthenticationModes actualModes = await provider.GetSupportedAuthenticationModesAsync(targetUri);

            Assert.Equal(expectedModes, actualModes);
        }

        [Fact]
        public async Task GitHubHostProvider_GetSupportedAuthenticationModes_OverrideInvalid_ReturnsDetectedValue()
        {
            var targetUri = new Uri("https://github.com");
            var expectedModes = GitHubConstants.DotDomAuthenticationModes;

            var context = new TestCommandContext
            {
                Settings =
                {
                    Environment =
                    {
                        Variables =
                        {
                            [GitHubConstants.EnvironmentVariables.AuthenticationModes] = "NOT-A-REAL-VALUE"
                        }
                    }
                }
            };

            var ghApiMock = new Mock<IGitHubRestApi>(MockBehavior.Strict);
            var ghAuthMock = new Mock<IGitHubAuthentication>(MockBehavior.Strict);

            var provider = new GitHubHostProvider(context, ghApiMock.Object, ghAuthMock.Object);

            AuthenticationModes actualModes = await provider.GetSupportedAuthenticationModesAsync(targetUri);

            Assert.Equal(expectedModes, actualModes);
        }

        [Fact]
        public async Task GitHubHostProvider_GetSupportedAuthenticationModes_OverrideNone_ReturnsDetectedValue()
        {
            var targetUri = new Uri("https://github.com");
            var expectedModes = GitHubConstants.DotDomAuthenticationModes;

            var context = new TestCommandContext
            {
                Settings =
                {
                    Environment =
                    {
                        Variables =
                        {
                            [GitHubConstants.EnvironmentVariables.AuthenticationModes] = AuthenticationModes.None.ToString()
                        }
                    }
                }
            };

            var ghApiMock = new Mock<IGitHubRestApi>(MockBehavior.Strict);
            var ghAuthMock = new Mock<IGitHubAuthentication>(MockBehavior.Strict);

            var provider = new GitHubHostProvider(context, ghApiMock.Object, ghAuthMock.Object);

            AuthenticationModes actualModes = await provider.GetSupportedAuthenticationModesAsync(targetUri);

            Assert.Equal(expectedModes, actualModes);
        }

        [Fact]
        public async Task GitHubHostProvider_GetSupportedAuthenticationModes_GitHubDotCom_ReturnsDotComModes()
        {
            var targetUri = new Uri("https://github.com");
            var expectedModes = GitHubConstants.DotDomAuthenticationModes;

            var provider = new GitHubHostProvider(new TestCommandContext());

            AuthenticationModes actualModes = await provider.GetSupportedAuthenticationModesAsync(targetUri);

            Assert.Equal(expectedModes, actualModes);
        }

        [Fact]
        public async Task GitHubHostProvider_GetSupportedAuthenticationModes_NotDotCom_OldInstanceNoPassword_ReturnsNone()
        {
            var targetUri = new Uri("https://ghe.io");
            var metaInfo = new GitHubMetaInfo
            {
                InstalledVersion = "0.1",
                VerifiablePasswordAuthentication = false
            };

            var expectedModes = AuthenticationModes.None;

            var context = new TestCommandContext();
            var ghAuthMock = new Mock<IGitHubAuthentication>(MockBehavior.Strict);

            var ghApiMock = new Mock<IGitHubRestApi>(MockBehavior.Strict);
            ghApiMock.Setup(x => x.GetMetaInfoAsync(targetUri)).ReturnsAsync(metaInfo);

            var provider = new GitHubHostProvider(context, ghApiMock.Object, ghAuthMock.Object);

            AuthenticationModes actualModes = await provider.GetSupportedAuthenticationModesAsync(targetUri);

            Assert.Equal(expectedModes, actualModes);
        }

        [Fact]
        public async Task GitHubHostProvider_GetSupportedAuthenticationModes_NotDotCom_OldInstanceWithPassword_ReturnsBasic()
        {
            var targetUri = new Uri("https://ghe.io");
            var metaInfo = new GitHubMetaInfo
            {
                InstalledVersion = "0.1",
                VerifiablePasswordAuthentication = true
            };

            var expectedModes = AuthenticationModes.Basic;

            var context = new TestCommandContext();
            var ghAuthMock = new Mock<IGitHubAuthentication>(MockBehavior.Strict);

            var ghApiMock = new Mock<IGitHubRestApi>(MockBehavior.Strict);
            ghApiMock.Setup(x => x.GetMetaInfoAsync(targetUri)).ReturnsAsync(metaInfo);

            var provider = new GitHubHostProvider(context, ghApiMock.Object, ghAuthMock.Object);

            AuthenticationModes actualModes = await provider.GetSupportedAuthenticationModesAsync(targetUri);

            Assert.Equal(expectedModes, actualModes);
        }

        [Fact]
        public async Task GitHubHostProvider_GetSupportedAuthenticationModes_NotDotCom_NewInstanceNoPassword_ReturnsOAuth()
        {
            var targetUri = new Uri("https://ghe.io");
            var metaInfo = new GitHubMetaInfo
            {
                InstalledVersion = "100.0",
                VerifiablePasswordAuthentication = false
            };

            var expectedModes = AuthenticationModes.OAuth;

            var context = new TestCommandContext();
            var ghAuthMock = new Mock<IGitHubAuthentication>(MockBehavior.Strict);

            var ghApiMock = new Mock<IGitHubRestApi>(MockBehavior.Strict);
            ghApiMock.Setup(x => x.GetMetaInfoAsync(targetUri)).ReturnsAsync(metaInfo);

            var provider = new GitHubHostProvider(context, ghApiMock.Object, ghAuthMock.Object);

            AuthenticationModes actualModes = await provider.GetSupportedAuthenticationModesAsync(targetUri);

            Assert.Equal(expectedModes, actualModes);
        }

        [Fact]
        public async Task GitHubHostProvider_GetSupportedAuthenticationModes_NotDotCom_NewInstanceWithPassword_ReturnsBasicAndOAuth()
        {
            var targetUri = new Uri("https://ghe.io");
            var metaInfo = new GitHubMetaInfo
            {
                InstalledVersion = "100.0",
                VerifiablePasswordAuthentication = true
            };

            var expectedModes = AuthenticationModes.Basic | AuthenticationModes.OAuth;

            var context = new TestCommandContext();
            var ghAuthMock = new Mock<IGitHubAuthentication>(MockBehavior.Strict);

            var ghApiMock = new Mock<IGitHubRestApi>(MockBehavior.Strict);
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

            var tokenValue = "OAUTH-TOKEN";
            var response = new OAuth2TokenResult(tokenValue, "bearer");

            var context = new TestCommandContext();

            var ghAuthMock = new Mock<IGitHubAuthentication>(MockBehavior.Strict);
            ghAuthMock.Setup(x => x.GetAuthenticationAsync(expectedTargetUri, It.IsAny<AuthenticationModes>()))
                      .ReturnsAsync(new AuthenticationPromptResult(AuthenticationModes.OAuth));

            ghAuthMock.Setup(x => x.GetOAuthTokenAsync(expectedTargetUri, It.IsAny<IEnumerable<string>>()))
                      .ReturnsAsync(response);

            var ghApiMock = new Mock<IGitHubRestApi>(MockBehavior.Strict);

            var provider = new GitHubHostProvider(context, ghApiMock.Object, ghAuthMock.Object);

            ICredential credential = await provider.GenerateCredentialAsync(input);

            Assert.NotNull(credential);
            Assert.Equal(Constants.OAuthTokenUserName, credential.UserName);
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
            var expectedPassword = "letmein123";
            IEnumerable<string> expectedPatScopes = new[]
            {
                GitHubConstants.TokenScopes.Gist,
                GitHubConstants.TokenScopes.Repo,
            };

            var patValue = "PERSONAL-ACCESS-TOKEN";
            var pat = new GitCredential(Constants.PersonalAccessTokenUserName, patValue);
            var response = new AuthenticationResult(GitHubAuthenticationResultType.Success, pat);

            var context = new TestCommandContext();

            var ghAuthMock = new Mock<IGitHubAuthentication>(MockBehavior.Strict);
            ghAuthMock.Setup(x => x.GetAuthenticationAsync(expectedTargetUri, It.IsAny<AuthenticationModes>()))
                      .ReturnsAsync(new AuthenticationPromptResult(new GitCredential(expectedUserName, expectedPassword)));

            var ghApiMock = new Mock<IGitHubRestApi>(MockBehavior.Strict);
            ghApiMock.Setup(x => x.CreatePersonalAccessTokenAsync(expectedTargetUri, expectedUserName, expectedPassword, null, It.IsAny<IEnumerable<string>>()))
                     .ReturnsAsync(response);

            var provider = new GitHubHostProvider(context, ghApiMock.Object, ghAuthMock.Object);

            ICredential credential = await provider.GenerateCredentialAsync(input);

            Assert.NotNull(credential);
            Assert.Equal(Constants.PersonalAccessTokenUserName, credential.UserName);
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
            var expectedPassword = "letmein123";
            var expectedAuthCode = "123456";
            IEnumerable<string> expectedPatScopes = new[]
            {
                GitHubConstants.TokenScopes.Gist,
                GitHubConstants.TokenScopes.Repo,
            };

            var patValue = "PERSONAL-ACCESS-TOKEN";
            var pat = new GitCredential(Constants.PersonalAccessTokenUserName, patValue);
            var response1 = new AuthenticationResult(GitHubAuthenticationResultType.TwoFactorApp);
            var response2 = new AuthenticationResult(GitHubAuthenticationResultType.Success, pat);

            var context = new TestCommandContext();

            var ghAuthMock = new Mock<IGitHubAuthentication>(MockBehavior.Strict);
            ghAuthMock.Setup(x => x.GetAuthenticationAsync(expectedTargetUri, It.IsAny<AuthenticationModes>()))
                      .ReturnsAsync(new AuthenticationPromptResult(new GitCredential(expectedUserName, expectedPassword)));
            ghAuthMock.Setup(x => x.GetTwoFactorCodeAsync(expectedTargetUri, false))
                      .ReturnsAsync(expectedAuthCode);

            var ghApiMock = new Mock<IGitHubRestApi>(MockBehavior.Strict);
            ghApiMock.Setup(x => x.CreatePersonalAccessTokenAsync(expectedTargetUri, expectedUserName, expectedPassword, null, It.IsAny<IEnumerable<string>>()))
                        .ReturnsAsync(response1);
            ghApiMock.Setup(x => x.CreatePersonalAccessTokenAsync(expectedTargetUri, expectedUserName, expectedPassword, expectedAuthCode, It.IsAny<IEnumerable<string>>()))
                        .ReturnsAsync(response2);

            var provider = new GitHubHostProvider(context, ghApiMock.Object, ghAuthMock.Object);

            ICredential credential = await provider.GenerateCredentialAsync(input);

            Assert.NotNull(credential);
            Assert.Equal(Constants.PersonalAccessTokenUserName, credential.UserName);
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
