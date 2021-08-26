using System;
using System.Text;
using Newtonsoft.Json;

namespace Microsoft.Git.CredentialManager.Authentication.OAuth.Json
{
    public class ErrorResponseJson
    {
        [JsonProperty("error", Required = Required.Always)]
        public string Error { get; set; }

        [JsonProperty("error_description")]
        public string Description { get; set; }

        [JsonProperty("error_uri")]
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
