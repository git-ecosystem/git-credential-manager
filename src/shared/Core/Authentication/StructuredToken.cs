using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitCredentialManager.Authentication
{
    public abstract class StructuredToken
    {
        private class JwtHeader
        {
            [JsonRequired]
            [JsonInclude]
            [JsonPropertyName("typ")]
            public string Type { get; private set; }
        }
        private class JwtPayload : StructuredToken
        {
            [JsonRequired]
            [JsonInclude]
            [JsonPropertyName("exp")]
            public long Expiry { get; private set; }

            public override bool IsExpired
            {
                get
                {
                    return Expiry < DateTimeOffset.Now.ToUnixTimeSeconds();
                }
            }
        }

        public abstract bool IsExpired { get; }

        public static bool TryCreate(string value, out StructuredToken jwt)
        {
            jwt = null;
            try
            {
                var parts = value.Split('.');
                if (parts.Length != 3)
                {
                    return false;
                }
                var header = JsonSerializer.Deserialize<JwtHeader>(Base64UrlConvert.Decode(parts[0]));
                if (!"JWT".Equals(header.Type, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                jwt = JsonSerializer.Deserialize<JwtPayload>(Base64UrlConvert.Decode(parts[1]));
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
