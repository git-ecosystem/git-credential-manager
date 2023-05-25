using System;
using System.Text.Json.Serialization;

namespace GitCredentialManager.Authentication.OAuth.Json
{
    public class DeviceAuthorizationEndpointResponseJson
    {
        [JsonRequired]
        [JsonPropertyName("device_code")]
        public string DeviceCode { get; set; }

        [JsonRequired]
        [JsonPropertyName("user_code")]
        public string UserCode { get; set; }

        [JsonRequired]
        [JsonPropertyName("verification_uri")]
        public Uri VerificationUri { get; set; }

        [JsonPropertyName("expires_in")]
        public long ExpiresIn { get; set; }

        [JsonPropertyName("interval")]
        public long PollingInterval { get; set; }

        public OAuth2DeviceCodeResult ToResult()
        {
            return new OAuth2DeviceCodeResult(DeviceCode, UserCode, VerificationUri, TimeSpan.FromSeconds(PollingInterval))
            {
                ExpiresIn = TimeSpan.FromSeconds(ExpiresIn)
            };
        }
    }
}
