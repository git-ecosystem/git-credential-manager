using GitCredentialManager.Authentication;
using Xunit;

namespace GitCredentialManager.Tests.Authentication
{
    public class AuthenticationBaseTests
    {
        [Theory]
        [InlineData("foo", "foo")]
        [InlineData("foo bar", "\"foo bar\"")]
        [InlineData("foo\nbar", "\"foo\nbar\"")]
        [InlineData("foo\rbar", "\"foo\rbar\"")]
        [InlineData("foo\tbar", "\"foo\tbar\"")]
        [InlineData("foo\" bar", "\"foo\\\" bar\"")]
        [InlineData("foo\"", "\"foo\\\"\"")]
        [InlineData("\"foo", "\"\\\"foo\"")]
        [InlineData("foo\\", "\"foo\\\\\"")]
        [InlineData("foo\\\"", "\"foo\\\\\\\"\"")]
        public void AuthenticationBase_QuoteCmdArg(string input, string expected)
        {
            string actual = AuthenticationBase.QuoteCmdArg(input);
            Assert.Equal(expected, actual);
        }
    }
}
