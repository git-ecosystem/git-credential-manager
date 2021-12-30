using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitCredentialManager;
using GitCredentialManager.Authentication.OAuth;
using GitCredentialManager.Tests.Objects;
using Moq;
using Xunit;

namespace GitLab.Tests
{
    public class GitLabHostProviderTests
    {
        [Theory]
        [InlineData("https", "gitlab.com", true)]
        [InlineData("http", "gitlab.com", true)]
        [InlineData("https", "gitlab.freedesktop.org", true)]
        [InlineData("https", "gitlab.gnome.org", true)]
        [InlineData("https", "github.com", false)]
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
    }
}
