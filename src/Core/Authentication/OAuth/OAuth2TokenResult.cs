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
}
