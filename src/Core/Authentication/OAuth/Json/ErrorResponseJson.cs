using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitCredentialManager.Authentication.OAuth.Json
{
    public class ErrorResponseJson
    {
        [JsonRequired]
        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonPropertyName("error_description")]
        public string Description { get; set; }

        [JsonPropertyName("error_uri")]
        public Uri Uri { get; set; }

        public OAuth2Exception ToException(Exception innerException = null)
        {
            var message = new StringBuilder(Error);

            if (!string.IsNullOrEmpty(Description))
            {
                message.AppendFormat(": {0}", Description);
            }

            if (Uri != null)
            {
                message.AppendFormat(" [{0}]", Uri);
            }

            return new OAuth2Exception(message.ToString(), innerException) {HelpLink = Uri?.ToString()};
        }
    }
}
