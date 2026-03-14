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

        public static bool TryCreate(string value, out StructuredToken token)
        {
            try
            {
                // elements of JWT structure "<header>.<payload>.<signature>"
                var parts = value.Split('.');
                if (parts.Length == 3)
                {
                    var header = JsonSerializer.Deserialize<JwtHeader>(Base64UrlConvert.Decode(parts[0]));
                    if ("JWT".Equals(header.Type, StringComparison.OrdinalIgnoreCase))
                    {
                        token = JsonSerializer.Deserialize<JwtPayload>(Base64UrlConvert.Decode(parts[1]));
                        return true;
                    }
                }
            }
            catch { }

            // invalid token data on content mismatch or deserializer exception
            token = null;
            return false;
        }
    }
}
