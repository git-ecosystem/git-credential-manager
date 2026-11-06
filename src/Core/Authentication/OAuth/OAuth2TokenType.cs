namespace GitCredentialManager.Authentication.OAuth;

/// <summary>
/// The type of token being requested or exchanged in an OAuth 2.0 flow.
/// </summary>
public record struct OAuth2TokenType(string Identifier)
{
    /// <summary>
    /// An OAuth 2.0 access token issued by an authorization server.
    /// </summary>
    public static readonly OAuth2TokenType AccessToken = new(OAuth2Constants.TokenTypes.AccessToken);

    /// <summary>
    /// An OAuth 2.0 refresh token issued by an authorization server.
    /// </summary>
    public static readonly OAuth2TokenType RefreshToken = new(OAuth2Constants.TokenTypes.RefreshToken);

    /// <summary>
    /// An ID Token as defined in Section 2 of the OpenID Connect Core 1.0 specification.
    /// </summary>
    public static readonly OAuth2TokenType IdToken = new(OAuth2Constants.TokenTypes.IdToken);

    /// <summary>
    /// A SAML 1.1 assertion.
    /// </summary>
    public static readonly OAuth2TokenType Saml1Assertion = new(OAuth2Constants.TokenTypes.Saml1Assertion);

    /// <summary>
    /// A SAML 2.0 assertion.
    /// </summary>
    public static readonly OAuth2TokenType Saml2Assertion = new(OAuth2Constants.TokenTypes.Saml2Assertion);

    /// <summary>
    /// A JWT token as defined in RFC 7519.
    /// </summary>
    public static readonly OAuth2TokenType JwtToken = new(OAuth2Constants.TokenTypes.JwtToken);
}
