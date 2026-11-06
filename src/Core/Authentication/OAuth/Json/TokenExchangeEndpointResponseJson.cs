using System.Text.Json.Serialization;

namespace GitCredentialManager.Authentication.OAuth.Json
{
    /// <summary>
    /// Token exchange response members defined in addition to those of a standard token
    /// endpoint response (RFC 8693, Section 2.2.1).
    /// </summary>
    /// <remarks>
    /// This intentionally does not derive from <see cref="TokenEndpointResponseJson"/> so that
    /// the common members are parsed by the (possibly provider-specific) token endpoint
    /// response parser, and only the extension members are parsed here.
    /// </remarks>
    public class TokenExchangeEndpointResponseJson
    {
        [JsonRequired]
        [JsonPropertyName("issued_token_type")]
        public string IssuedTokenType { get; set; }
    }
}
