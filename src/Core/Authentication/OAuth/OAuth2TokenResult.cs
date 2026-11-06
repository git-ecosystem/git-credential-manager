using System;

namespace GitCredentialManager.Authentication.OAuth
{
    public class OAuth2TokenResult
    {
        public OAuth2TokenResult(string accessToken, string tokenType)
        {
            AccessToken = accessToken;
            TokenType = tokenType;
        }

        public string AccessToken { get; }

        public string TokenType { get; }

        public string RefreshToken { get; set; }

        public TimeSpan? ExpiresIn { get; set; }

        public string[] Scopes { get; set; }
    }

    public class OAuth2TokenExchangeResult : OAuth2TokenResult
    {
        public OAuth2TokenExchangeResult(OAuth2TokenResult tokenResult, OAuth2TokenType issuedTokenType)
            : base(tokenResult.AccessToken, tokenResult.TokenType)
        {
            IssuedTokenType = issuedTokenType;
            RefreshToken = tokenResult.RefreshToken;
            ExpiresIn = tokenResult.ExpiresIn;
            Scopes = tokenResult.Scopes;
        }

        /// <summary>
        /// The type of the token issued in <see cref="OAuth2TokenResult.AccessToken"/>, which may
        /// not be an OAuth 2.0 access token.
        /// </summary>
        public OAuth2TokenType IssuedTokenType { get; }
    }
}
