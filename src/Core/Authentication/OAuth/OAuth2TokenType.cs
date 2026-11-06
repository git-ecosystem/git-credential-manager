using System;

namespace GitCredentialManager.Authentication.OAuth;

/// <summary>
/// The type of token being requested or exchanged in an OAuth 2.0 flow.
/// </summary>
public enum OAuth2TokenType
{
    /// <summary>
    /// An OAuth 2.0 access token issued by an authorization server.
    /// </summary>
    AccessToken,

    /// <summary>
    /// An OAuth 2.0 refresh token issued by an authorization server.
    /// </summary>
    RefreshToken,

    /// <summary>
    /// An ID Token as defined in Section 2 of the OpenID Connect Core 1.0 specification.
    /// </summary>
    IdToken,

    /// <summary>
    /// A SAML 1.1 assertion.
    /// </summary>
    Saml1Assertion,

    /// <summary>
    /// A SAML 2.0 assertion.
    /// </summary>
    Saml2Assertion,
}

public static class OAuth2TokenTypeExtensions
{
    /// <summary>
    /// Get the wire value for the token type request parameter.
    /// </summary>
    public static string GetParameterValue(this OAuth2TokenType tokenType)
    {
        switch (tokenType)
        {
            case OAuth2TokenType.AccessToken:
                return OAuth2Constants.TokenTypes.AccessToken;
            case OAuth2TokenType.RefreshToken:
                return OAuth2Constants.TokenTypes.RefreshToken;
            case OAuth2TokenType.IdToken:
                return OAuth2Constants.TokenTypes.IdToken;
            case OAuth2TokenType.Saml1Assertion:
                return OAuth2Constants.TokenTypes.Saml1Assertion;
            case OAuth2TokenType.Saml2Assertion:
                return OAuth2Constants.TokenTypes.Saml2Assertion;
            default:
                throw new ArgumentOutOfRangeException(nameof(tokenType), tokenType, "Unknown token type.");
        }
    }

    /// <summary>
    /// Try to parse a token type wire value into an <see cref="OAuth2TokenType"/>.
    /// </summary>
    public static bool TryParse(string value, out OAuth2TokenType tokenType)
    {
        tokenType = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        switch (value.ToLowerInvariant())
        {
            case OAuth2Constants.TokenTypes.AccessToken:
                tokenType = OAuth2TokenType.AccessToken;
                return true;

            case OAuth2Constants.TokenTypes.RefreshToken:
                tokenType = OAuth2TokenType.RefreshToken;
                return true;

            case OAuth2Constants.TokenTypes.IdToken:
                tokenType = OAuth2TokenType.IdToken;
                return true;

            case OAuth2Constants.TokenTypes.Saml1Assertion:
                tokenType = OAuth2TokenType.Saml1Assertion;
                return true;

            case OAuth2Constants.TokenTypes.Saml2Assertion:
                tokenType = OAuth2TokenType.Saml2Assertion;
                return true;

            default:
                return false;
        }
    }
}
