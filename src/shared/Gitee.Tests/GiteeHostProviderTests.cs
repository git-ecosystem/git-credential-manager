using System;
using System.Collections.Generic;
using GitCredentialManager;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace Gitee.Tests
{
    public class GiteeHostProviderTests
    {
        [Theory]
        [InlineData("https", "gitee.com", true)]
        [InlineData("http", "gitee.com", true)]
        [InlineData("https", "gitee.example.com", true)]
        [InlineData("https", "github.com", false)]
        [InlineData("https", "github.example.com", false)]
        public void GiteeHostProvider_IsSupported(string protocol, string host, bool expected)
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = protocol,
                ["host"] = host,
            });

            var provider = new GiteeHostProvider(new TestCommandContext());
            Assert.Equal(expected, provider.IsSupported(input));
        }

        [Fact]
        public void GiteeHostProvider_GetSupportedAuthenticationModes_DotCom_ReturnsDotComModes()
        {
            Uri targetUri = GiteeConstants.GiteeDotCom;
            AuthenticationModes expected = GiteeConstants.DotComAuthenticationModes;

            var context = new TestCommandContext();
            var provider = new GiteeHostProvider(context);
            AuthenticationModes actual = provider.GetSupportedAuthenticationModes(targetUri);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GiteeHostProvider_GetSupportedAuthenticationModes_Custom_NoOAuthConfig_ReturnsBasicPat()
        {
            var targetUri = new Uri("https://gitee.example.com");
            var expected = AuthenticationModes.Basic
                                          | AuthenticationModes.Pat;

            var context = new TestCommandContext();
            var provider = new GiteeHostProvider(context);
            AuthenticationModes actual = provider.GetSupportedAuthenticationModes(targetUri);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GiteeHostProvider_GetSupportedAuthenticationModes_Custom_WithOAuthConfig_ReturnsBasicPatBrowser()
        {
            var targetUri = new Uri("https://gitee.example.com");
            var expected = AuthenticationModes.Basic
                                          | AuthenticationModes.Pat
                                          | AuthenticationModes.Browser;

            var context = new TestCommandContext();
            context.Environment.Variables[GiteeConstants.EnvironmentVariables.DevOAuthClientId] = "abcdefg1234567";

            var provider = new GiteeHostProvider(context);
            AuthenticationModes actual = provider.GetSupportedAuthenticationModes(targetUri);

            Assert.Equal(expected, actual);
        }
    }
}
