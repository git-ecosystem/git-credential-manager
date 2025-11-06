using Atlassian.Bitbucket.DataCenter;
using GitCredentialManager;
using Moq;
using System;
using Xunit;

namespace Atlassian.Bitbucket.Tests
{
    public class BitbucketHelperTest
    {


        private Mock<ISettings> settings = new Mock<ISettings>(MockBehavior.Loose);

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("    ", false)]
        [InlineData("bitbucket.org", true)]
        [InlineData("BITBUCKET.ORG", true)]
        [InlineData("BiTbUcKeT.OrG", true)]
        [InlineData("bitbucket.example.com", false)]
        [InlineData("bitbucket.example.org", false)]
        [InlineData("bitbucket.org.com", false)]
        [InlineData("bitbucket.org.org", false)]
        public void BitbucketHelper_IsBitbucketOrg_StringHost(string str, bool expected)
        {
            bool actual = BitbucketHelper.IsBitbucketOrg(str);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("http://bitbucket.org", true)]
        [InlineData("https://bitbucket.org", true)]
        [InlineData("http://bitbucket.org/path", true)]
        [InlineData("https://bitbucket.org/path", true)]
        [InlineData("http://BITBUCKET.ORG", true)]
        [InlineData("https://BITBUCKET.ORG", true)]
        [InlineData("http://BITBUCKET.ORG/PATH", true)]
        [InlineData("https://BITBUCKET.ORG/PATH", true)]
        [InlineData("http://BiTbUcKeT.OrG", true)]
        [InlineData("https://BiTbUcKeT.OrG", true)]
        [InlineData("http://BiTbUcKeT.OrG/pAtH", true)]
        [InlineData("https://BiTbUcKeT.OrG/pAtH", true)]
        [InlineData("http://bitbucket.example.com", false)]
        [InlineData("https://bitbucket.example.com", false)]
        [InlineData("http://bitbucket.example.com/path", false)]
        [InlineData("https://bitbucket.example.com/path", false)]
        [InlineData("http://bitbucket.example.org", false)]
        [InlineData("https://bitbucket.example.org", false)]
        [InlineData("http://bitbucket.example.org/path", false)]
        [InlineData("https://bitbucket.example.org/path", false)]
        [InlineData("http://bitbucket.org.com", false)]
        [InlineData("https://bitbucket.org.com", false)]
        [InlineData("http://bitbucket.org.com/path", false)]
        [InlineData("https://bitbucket.org.com/path", false)]
        [InlineData("http://bitbucket.org.org", false)]
        [InlineData("https://bitbucket.org.org", false)]
        [InlineData("http://bitbucket.org.org/path", false)]
        [InlineData("https://bitbucket.org.org/path", false)]
        public void BitbucketHelper_IsBitbucketOrg_Uri(string str, bool expected)
        {
            bool actual = BitbucketHelper.IsBitbucketOrg(new Uri(str));
            Assert.Equal(expected, actual);
        }

        [Theory]
        // old behavior
        [InlineData("http://bitbucket.org", null, "http://bitbucket.org:80")]
        [InlineData("https://bitbucket.org", null, "https://bitbucket.org:443")]
        [InlineData("https://bitbucket.org/project/repo.git", null, "https://bitbucket.org:443/project")]
        // with http path
        [InlineData("http://bitbucket.org", "/bitbucket", "http://bitbucket.org:80/bitbucket")]
        [InlineData("https://bitbucket.org", "/bitbucket", "https://bitbucket.org:443/bitbucket")]
        // usehttppath takes preference over httpPath
        [InlineData("https://bitbucket.org/project/repo.git", "/bitbucket", "https://bitbucket.org:443/project")]
        public void BitbucketHelper_GetBaseUri(string uri, string httpPath, string expected)
        {

            settings.Setup(s => s.RemoteUri).Returns(new Uri(uri));
            if(httpPath != null)
            {
                MockHttpPath(httpPath);
            }
            var actual = BitbucketHelper.GetBaseUri(settings.Object);
            Assert.Equal(expected, actual);
        }

        private string MockHttpPath(string value)
        {
            settings.Setup(s => s.TryGetSetting(
                DataCenterConstants.EnvironmentVariables.HttpPath,
                Constants.GitConfiguration.Credential.SectionName, DataCenterConstants.GitConfiguration.Credential.HttpPath,
                out value)).Returns(true);
            return value;
        }
    }
}