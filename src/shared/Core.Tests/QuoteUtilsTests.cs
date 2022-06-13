using Xunit;

namespace GitCredentialManager.Tests
{
    public class QuoteUtilsTests
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
        public void QuoteUtils_QuoteCmdArg(string input, string expected)
        {
            string actual = QuoteUtils.QuoteCmdArg(input);
            Assert.Equal(expected, actual);
        }
    }
}