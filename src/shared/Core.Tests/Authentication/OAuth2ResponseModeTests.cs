using GitCredentialManager.Authentication.OAuth;
using Xunit;

namespace GitCredentialManager.Tests.Authentication;

public class OAuth2ResponseModeTests
{
    [Theory]
    [InlineData(OAuth2ResponseMode.Default, null)]
    [InlineData(OAuth2ResponseMode.Query, "query")]
    [InlineData(OAuth2ResponseMode.Fragment, "fragment")]
    [InlineData(OAuth2ResponseMode.FormPost, "form_post")]
    public void OAuth2ResponseMode_GetParameterValue(OAuth2ResponseMode mode, string expected)
    {
        Assert.Equal(expected, mode.GetParameterValue());
    }

    [Theory]
    [InlineData("query", OAuth2ResponseMode.Query)]
    [InlineData("Query", OAuth2ResponseMode.Query)]
    [InlineData("fragment", OAuth2ResponseMode.Fragment)]
    [InlineData("FRAGMENT", OAuth2ResponseMode.Fragment)]
    [InlineData("form_post", OAuth2ResponseMode.FormPost)]
    [InlineData("FORM_POST", OAuth2ResponseMode.FormPost)]
    [InlineData("formpost", OAuth2ResponseMode.FormPost)]
    public void OAuth2ResponseMode_TryParse_Valid(string value, OAuth2ResponseMode expected)
    {
        Assert.True(OAuth2ResponseModeExtensions.TryParse(value, out OAuth2ResponseMode actual));
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("web_message")]
    [InlineData("unknown")]
    public void OAuth2ResponseMode_TryParse_Invalid_ReturnsFalse(string value)
    {
        Assert.False(OAuth2ResponseModeExtensions.TryParse(value, out _));
    }
}
