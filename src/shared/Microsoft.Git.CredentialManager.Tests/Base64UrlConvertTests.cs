using Xunit;

namespace GitCredentialManager.Tests
{
    public class Base64UrlConvertTests
    {
        [Theory]
        [InlineData(new byte[0], "")]
        [InlineData(new byte[]{4}, "BA==")]
        [InlineData(new byte[]{4,5}, "BAU=")]
        [InlineData(new byte[]{4,5,6}, "BAUG")]
        [InlineData(new byte[]{4,5,6,7}, "BAUGBw==")]
        [InlineData(new byte[]{4,5,6,7,8}, "BAUGBwg=")]
        [InlineData(new byte[]{4,5,6,7,8,9}, "BAUGBwgJ")]
        public void Base64UrlConvert_Encode_WithPadding(byte[] data, string expected)
        {
            string actual = Base64UrlConvert.Encode(data, includePadding: true);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(new byte[0], "")]
        [InlineData(new byte[]{4}, "BA")]
        [InlineData(new byte[]{4,5}, "BAU")]
        [InlineData(new byte[]{4,5,6}, "BAUG")]
        [InlineData(new byte[]{4,5,6,7}, "BAUGBw")]
        [InlineData(new byte[]{4,5,6,7,8}, "BAUGBwg")]
        [InlineData(new byte[]{4,5,6,7,8,9}, "BAUGBwgJ")]
        public void Base64UrlConvert_Encode_WithoutPadding(byte[] data, string expected)
        {
            string actual = Base64UrlConvert.Encode(data, includePadding: false);
            Assert.Equal(expected, actual);
        }
    }
}
