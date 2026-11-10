using GitCredentialManager.Authentication.OAuth;
using Xunit;

namespace GitCredentialManager.Tests.Authentication;

public class OAuth2TokenTypeTests
{
    [Theory]
    [InlineData(OAuth2TokenType.AccessToken, "urn:ietf:params:oauth:token-type:access_token")]
    [InlineData(OAuth2TokenType.RefreshToken, "urn:ietf:params:oauth:token-type:refresh_token")]
    [InlineData(OAuth2TokenType.IdToken, "urn:ietf:params:oauth:token-type:id_token")]
    [InlineData(OAuth2TokenType.Saml1Assertion, "urn:ietf:params:oauth:token-type:saml1")]
    [InlineData(OAuth2TokenType.Saml2Assertion, "urn:ietf:params:oauth:token-type:saml2")]
    public void OAuth2TokenType_GetParameterValue(OAuth2TokenType tokenType, string expected)
    {
        Assert.Equal(expected, tokenType.GetParameterValue());
    }

    [Theory]
    [InlineData("urn:ietf:params:oauth:token-type:access_token", OAuth2TokenType.AccessToken)]
    [InlineData("urn:ietf:params:oauth:token-type:refresh_token", OAuth2TokenType.RefreshToken)]
    [InlineData("urn:ietf:params:oauth:token-type:id_token", OAuth2TokenType.IdToken)]
    [InlineData("urn:ietf:params:oauth:token-type:saml1", OAuth2TokenType.Saml1Assertion)]
    [InlineData("urn:ietf:params:oauth:token-type:saml2", OAuth2TokenType.Saml2Assertion)]
    public void OAuth2TokenType_TryParse_Valid(string value, OAuth2TokenType expected)
    {
        Assert.True(OAuth2TokenTypeExtensions.TryParse(value, out OAuth2TokenType actual));
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("foo_token")]
    [InlineData("unknown")]
    public void OAuth2TokenType_TryParse_Invalid_ReturnsFalse(string value)
    {
        Assert.False(OAuth2TokenTypeExtensions.TryParse(value, out _));
    }
}
