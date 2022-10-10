using System;
using Xunit;

namespace Atlassian.Bitbucket.Tests
{
    public class BitbucketHelperTest
    {
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
    }
}