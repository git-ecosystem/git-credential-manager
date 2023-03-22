using GitCredentialManager;
using Xunit;

namespace Core.Tests;

public class Trace2MessageTests
{
    [Theory]
    [InlineData(0.013772,     "  0.013772 ")]
    [InlineData(26.316083,    " 26.316083 ")]
    [InlineData(100.316083,   "100.316083 ")]
    [InlineData(1000.316083,  "1000.316083")]
    public void BuildTimeSpan_Match_Returns_Expected_String(double input, string expected)
    {
        var actual = Trace2Message.BuildTimeSpan(input);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BuildRepoSpan_Match_Returns_Expected_String()
    {
        var input = 1;
        var expected = " r1  ";
        var actual = Trace2Message.BuildRepoSpan(input);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("foo",               " foo         ")]
    [InlineData("foobar",            " foobar      ")]
    [InlineData("foo_bar_baz",       " foo_bar_baz ")]
    [InlineData("foobarbazfoo",      " foobarbazfo ")]
    public void BuildCategorySpan_Match_Returns_Expected_String(string input, string expected)
    {
        var actual = Trace2Message.BuildCategorySpan(input);
        Assert.Equal(expected, actual);
    }
}
