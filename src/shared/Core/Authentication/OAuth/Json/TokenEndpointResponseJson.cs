using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitCredentialManager.Authentication.OAuth.Json
{
    public class TokenEndpointResponseJson
    {
        [JsonRequired]
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonRequired]
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public long? ExpiresIn { get; set; }

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonPropertyName("scope")]
        public virtual string Scope { get; set; }

        public OAuth2TokenResult ToResult()
        {
            return new OAuth2TokenResult(AccessToken, TokenType)
            {
                ExpiresIn = ExpiresIn.HasValue ? TimeSpan.FromSeconds(ExpiresIn.Value) : null,
                RefreshToken = RefreshToken,
                Scopes = Scope?.Split(' ')
            };
        }
    }
}
