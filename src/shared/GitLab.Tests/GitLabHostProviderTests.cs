using System;
using System.Collections.Generic;
using GitCredentialManager;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace GitLab.Tests
{
    public class GitLabHostProviderTests
    {
        [Theory]
        [InlineData("https", "gitlab.com", true)]
        [InlineData("http", "gitlab.com", true)]
        [InlineData("https", "gitlab.example.com", true)]
        [InlineData("https", "github.com", false)]
        [InlineData("https", "github.example.com", false)]
        public void GitLabHostProvider_IsSupported(string protocol, string host, bool expected)
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = protocol,
                ["host"] = host,
            });

            var provider = new GitLabHostProvider(new TestCommandContext());
            Assert.Equal(expected, provider.IsSupported(input));
        }

        [Fact]
        public void GitLabHostProvider_GetSupportedAuthenticationModes_DotCom_ReturnsDotComModes()
        {
            Uri targetUri = GitLabConstants.GitLabDotCom;
            AuthenticationModes expected = GitLabConstants.DotComAuthenticationModes;

            var context = new TestCommandContext();
            var provider = new GitLabHostProvider(context);
            AuthenticationModes actual = provider.GetSupportedAuthenticationModes(targetUri);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GitLabHostProvider_GetSupportedAuthenticationModes_Custom_NoOAuthConfig_ReturnsBasicPat()
        {
            var targetUri = new Uri("https://gitlab.example.com");
            var expected = AuthenticationModes.Basic
                                          | AuthenticationModes.Pat;

            var context = new TestCommandContext();
            var provider = new GitLabHostProvider(context);
            AuthenticationModes actual = provider.GetSupportedAuthenticationModes(targetUri);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GitLabHostProvider_GetSupportedAuthenticationModes_Custom_WithOAuthConfig_ReturnsBasicPatBrowser()
        {
            var targetUri = new Uri("https://gitlab.example.com");
            var expected = AuthenticationModes.Basic
                                          | AuthenticationModes.Pat
                                          | AuthenticationModes.Browser;

            var context = new TestCommandContext();
            context.Environment.Variables[GitLabConstants.EnvironmentVariables.DevOAuthClientId] = "abcdefg1234567";

            var provider = new GitLabHostProvider(context);
            AuthenticationModes actual = provider.GetSupportedAuthenticationModes(targetUri);

            Assert.Equal(expected, actual);
        }
    }
}
