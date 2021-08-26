using System;
using Newtonsoft.Json;

namespace Microsoft.Git.CredentialManager.Authentication.OAuth.Json
{
    public class TokenEndpointResponseJson
    {
        [JsonProperty("access_token", Required = Required.Always)]
        public string AccessToken { get; set; }

        [JsonProperty("token_type", Required = Required.Always)]
        public string TokenType { get; set; }

        [JsonProperty("expires_in")]
        [JsonConverter(typeof(TimeSpanSecondsConverter))]
        public TimeSpan? ExpiresIn { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("scope")]
        public virtual string Scope { get; set; }

        public OAuth2TokenResult ToResult()
        {
            return new OAuth2TokenResult(AccessToken, TokenType)
            {
                ExpiresIn = ExpiresIn,
                RefreshToken = RefreshToken,
                Scopes = Scope?.Split(' ')
            };
        }
    }
}
