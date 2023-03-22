using GitCredentialManager;
using Xunit;

namespace Core.Tests;

public class Trace2MessageTests
{
    [Theory]
    [InlineData(0.013772,    "  0.013772 ")]
    [InlineData(26.316083,   " 26.316083 ")]
    [InlineData(100.316083,  "100.316083 ")]
    public void BuildTimeSpan_Match_Returns_Expected_String(double input, string expected)
    {
        var actual = Trace2Message.BuildTimeSpan(input);
        Assert.Equal(expected, actual);
    }
}
