using System;
using Newtonsoft.Json;

namespace GitCredentialManager.Authentication.OAuth.Json
{
    public class DeviceAuthorizationEndpointResponseJson
    {
        [JsonProperty("device_code", Required = Required.Always)]
        public string DeviceCode { get; set; }

        [JsonProperty("user_code", Required = Required.Always)]
        public string UserCode { get; set; }

        [JsonProperty("verification_uri", Required = Required.Always)]
        public Uri VerificationUri { get; set; }

        [JsonProperty("expires_in")]
        public long ExpiresIn { get; set; }

        [JsonProperty("interval")]
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
