using System;
using System.Buffers.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitCredentialManager.Authentication
{
    public partial class JsonWebToken : IToken
    {
        public bool IsExpired
        {
            get
            {
                return Payload.Expiry != null && Payload.Expiry < DateTimeOffset.Now.ToUnixTimeSeconds();
            }
        }
        public string Type => "Bearer";
        public string Value { get; }

        public class HeaderData
        {
            [JsonRequired]
            [JsonInclude]
            [JsonPropertyName("typ")]
            public string Type { get; internal set; }
        }

        public class PayloadData
        {
            [JsonInclude]
            [JsonPropertyName("exp")]
            public long? Expiry { get; internal set; }
        }


        protected HeaderData Header { get; }
        protected PayloadData Payload { get; }
        protected string Signature { get; }

        public JsonWebToken(string value, HeaderData header, PayloadData payload, string signature)
        {
            Value = value;
            Header = header;
            Payload = payload;
            Signature = signature;
        }


        [JsonSerializable(typeof(HeaderData))]
        private partial class HeaderContext : JsonSerializerContext { }

        [JsonSerializable(typeof(PayloadData))]
        private partial class PayloadContext : JsonSerializerContext { }


        public static bool TryCreate(string value, out JsonWebToken token)
        {
            try
            {
                // elements of JWT structure "<header>.<payload>.<signature>"
                var parts = value.Split('.');
                if (parts.Length == 2 || parts.Length == 3)
                {
                    var header = JsonSerializer.Deserialize(Base64Url.DecodeFromChars(parts[0]), HeaderContext.Default.HeaderData);
                    if ("JWT".Equals(header.Type, StringComparison.OrdinalIgnoreCase))
                    {
                        var payload = JsonSerializer.Deserialize(Base64Url.DecodeFromChars(parts[1]), PayloadContext.Default.PayloadData);
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
