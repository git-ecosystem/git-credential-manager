using System;
using System.Buffers.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitCredentialManager.Authentication
{
    public partial class JsonWebToken : IToken
    {
        public static readonly string JwtTokenType = "JWT";

        public bool IsExpired => payload.Expiry != null && payload.Expiry < DateTimeOffset.Now.ToUnixTimeSeconds();
        public string Type => JwtTokenType;
        public string Value => _value;

        protected class Header
        {
            [JsonRequired]
            [JsonInclude]
            [JsonPropertyName("typ")]
            public string Type { get; internal set; }
        }

        protected class Payload
        {
            [JsonInclude]
            [JsonPropertyName("exp")]
            public long? Expiry { get; internal set; }
        }


        readonly Header header;
        readonly Payload payload;
        readonly string signature;
        readonly private string _value;

        protected JsonWebToken(string value, Header header, Payload payload, string signature)
        {
            _value = value;
            this.header = header;
            this.payload = payload;
            this.signature = signature;
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
                    if (JwtTokenType.Equals(header.Type, StringComparison.OrdinalIgnoreCase))
                    {
                        var payload = JsonSerializer.Deserialize(Base64Url.DecodeFromChars(parts[1]), PayloadDto.Default.Payload);
                        token = new JsonWebToken(value, header, payload, parts.Length > 2 ? parts[2] : null);
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
