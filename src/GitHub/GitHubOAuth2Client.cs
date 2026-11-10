using System;
using System.Net.Http;
using System.Text.Json.Serialization;
using GitCredentialManager;
using GitCredentialManager.Authentication.OAuth;
using GitCredentialManager.Authentication.OAuth.Json;

namespace GitHub
{
    public class GitHubOAuth2Client : OAuth2Client
    {
        public GitHubOAuth2Client(HttpClient httpClient, ISettings settings, Uri baseUri)
            : base(httpClient, CreateEndpoints(baseUri),
                GetClientId(settings), GetRedirectUri(settings, baseUri), GetClientSecret(settings)) { }

        protected override bool TryCreateTokenEndpointResult(string json, out OAuth2TokenResult result)
        {
            // We override the token endpoint response parsing because the GitHub auth server returns
            // the 'scope' property as a comma-separated list of scopes, rather than the space-separated
            // format as defined in RFC 6749 section 3.3.
            if (TryDeserializeJson(json,
                    GitHubOAuthJsonContext.Default.GitHubTokenEndpointResponseJson,
                    out GitHubTokenEndpointResponseJson jsonObj))
            {
                result = jsonObj.ToResult();
                return true;
            }

            result = null;
            return false;
        }

        private static OAuth2ServerEndpoints CreateEndpoints(Uri uri)
        {
            // Ensure that the base URI is normalized to support Gist subdomains
            Uri baseUri = GitHubHostProvider.NormalizeUri(uri);

            Uri authEndpoint = new Uri(baseUri, GitHubConstants.OAuthAuthorizationEndpointRelativeUri);
            Uri tokenEndpoint = new Uri(baseUri, GitHubConstants.OAuthTokenEndpointRelativeUri);
            Uri deviceAuthEndpoint = new Uri(baseUri, GitHubConstants.OAuthDeviceEndpointRelativeUri);

            return new OAuth2ServerEndpoints(authEndpoint, tokenEndpoint)
            {
                DeviceAuthorizationEndpoint = deviceAuthEndpoint
            };
        }

        private static string GetClientId(ISettings settings)
        {
            // Check for developer override value
            if (settings.TryGetSetting(
                GitHubConstants.EnvironmentVariables.DevOAuthClientId,
                Constants.GitConfiguration.Credential.SectionName, GitHubConstants.GitConfiguration.Credential.DevOAuthClientId,
                out string clientId))
            {
                return clientId;
            }

            return GitHubConstants.OAuthClientId;
        }

        private static Uri GetRedirectUri(ISettings settings, Uri targetUri)
        {
            // Check for developer override value
            if (settings.TryGetSetting(
                GitHubConstants.EnvironmentVariables.DevOAuthRedirectUri,
                Constants.GitConfiguration.Credential.SectionName, GitHubConstants.GitConfiguration.Credential.DevOAuthRedirectUri,
                out string redirectUriStr) && Uri.TryCreate(redirectUriStr, UriKind.Absolute, out Uri redirectUri))
            {
                return redirectUri;
            }

            // Only GitHub.com supports the new OAuth redirect URI today
            return GitHubHostProvider.IsGitHubDotCom(targetUri)
                ? GitHubConstants.OAuthRedirectUri
                : GitHubConstants.OAuthLegacyRedirectUri;
        }

        private static string GetClientSecret(ISettings settings)
        {
            // Check for developer override value
            if (settings.TryGetSetting(
                GitHubConstants.EnvironmentVariables.DevOAuthClientSecret,
                Constants.GitConfiguration.Credential.SectionName, GitHubConstants.GitConfiguration.Credential.DevOAuthClientSecret,
                out string clientSecret))
            {
                return clientSecret;
            }

            return GitHubConstants.OAuthClientSecret;
        }
    }

    [JsonSerializable(typeof(GitHubTokenEndpointResponseJson))]
    public partial class GitHubOAuthJsonContext : JsonSerializerContext;

    public class GitHubTokenEndpointResponseJson : TokenEndpointResponseJson
    {
        public override OAuth2TokenResult ToResult()
        {
            return new OAuth2TokenResult(AccessToken, TokenType)
            {
                ExpiresIn = ExpiresIn.HasValue ? TimeSpan.FromSeconds(ExpiresIn.Value) : null,
                RefreshToken = RefreshToken,
                // GitHub returns a comma-separated list of scopes, rather than the expected
                // space-delimited list as defined in RFC 6749 section 3.3.
                Scopes = Scope?.Split(',')
            };
        }
    }
}
