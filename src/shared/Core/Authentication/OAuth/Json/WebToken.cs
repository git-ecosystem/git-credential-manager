using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitCredentialManager.Authentication.Oauth.Json
{
    public class WebToken(WebToken.TokenHeader header, WebToken.TokenPayload payload, string signature)
    {
        public class TokenHeader
        {
            [JsonRequired]
            [JsonInclude]
            [JsonPropertyName("typ")]
            public string Type { get; private set; }
        }
        public class TokenPayload
        {
            [JsonRequired]
            [JsonInclude]
            [JsonPropertyName("exp")]
            public long Expiry { get; private set; }
        }
        public TokenHeader Header { get; } = header;
        public TokenPayload Payload { get; } = payload;
        public string Signature { get; } = signature;

        public bool IsExpired
        {
            get
            {
                return Payload.Expiry < DateTimeOffset.Now.ToUnixTimeSeconds();
            }
        }

        static public bool TryCreate(string value, out WebToken jwt)
        {
            jwt = null;
            try
            {
                var parts = value.Split('.');
                if (parts.Length != 3)
                {
                    return false;
                }
                var header = JsonSerializer.Deserialize<TokenHeader>(Base64UrlConvert.Decode(parts[0]));
                if (!"JWT".Equals(header.Type))
                {
                    return false;
                }
                var payload = JsonSerializer.Deserialize<TokenPayload>(Base64UrlConvert.Decode(parts[1]));
                jwt = new WebToken(header, payload, parts[2]);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
