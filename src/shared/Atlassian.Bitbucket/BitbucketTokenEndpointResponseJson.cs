using GitCredentialManager.Authentication.OAuth.Json;
using Newtonsoft.Json;

namespace Atlassian.Bitbucket 
{
        public class BitbucketTokenEndpointResponseJson : TokenEndpointResponseJson
        {
            // Bitbucket uses "scopes" for the scopes property name rather than the standard "scope" name
            [JsonProperty("scopes")]
            public override string Scope { get; set; }
        }
}