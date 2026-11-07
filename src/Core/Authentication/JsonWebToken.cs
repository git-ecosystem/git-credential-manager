using System;
using System.Buffers.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitCredentialManager.Authentication
{
    public partial class JsonWebToken
    {
        public static readonly string Type = "JWT";

        public long? Expiry { get; }
        public string Value { get; }

        class Header
        {
            [JsonRequired]
            [JsonInclude]
            [JsonPropertyName("typ")]
            public string Type { get; internal set; }
        }

        class Payload
        {
            [JsonInclude]
            [JsonPropertyName("exp")]
            public long? Expiry { get; internal set; }
        }

        JsonWebToken(long? expiry, string value)
        {
            Expiry = expiry;
            Value = value;
        }


        [JsonSerializable(typeof(Header))]
        private partial class HeaderDto : JsonSerializerContext { }

        [JsonSerializable(typeof(Payload))]
        private partial class PayloadDto : JsonSerializerContext { }


        public static bool TryCreate(string value, out JsonWebToken token)
        {
            try
            {
                // elements of JWT structure "<header>.<payload>.<signature>"
                var parts = value.Split('.');
                if (parts.Length == 2 || parts.Length == 3)
                {
                    var header = JsonSerializer.Deserialize(Base64Url.DecodeFromChars(parts[0]), HeaderDto.Default.Header);
                    if (Type.Equals(header.Type, StringComparison.OrdinalIgnoreCase))
                    {
                        var payload = JsonSerializer.Deserialize(Base64Url.DecodeFromChars(parts[1]), PayloadDto.Default.Payload);
                        token = new JsonWebToken(payload.Expiry, value);
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
