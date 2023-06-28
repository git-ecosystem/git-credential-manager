using System;
using System.Net.Http;
using GitCredentialManager;
using GitCredentialManager.Authentication.OAuth;

namespace GitLab
{
    public class GitLabOAuth2Client : OAuth2Client
    {
        public GitLabOAuth2Client(HttpClient httpClient, ISettings settings, Uri baseUri, ITrace2 trace2)
            : base(httpClient, CreateEndpoints(baseUri),
                GetClientId(settings), trace2, GetRedirectUri(settings), GetClientSecret(settings))
        { }

        private static OAuth2ServerEndpoints CreateEndpoints(Uri baseUri)
        {
            Uri authEndpoint = new Uri(baseUri, GitLabConstants.OAuthAuthorizationEndpointRelativeUri);
            Uri tokenEndpoint = new Uri(baseUri, GitLabConstants.OAuthTokenEndpointRelativeUri);

            return new OAuth2ServerEndpoints(authEndpoint, tokenEndpoint);
        }

        private static Uri GetRedirectUri(ISettings settings)
        {
            // Check for developer override value
            if (settings.TryGetSetting(
                GitLabConstants.EnvironmentVariables.DevOAuthRedirectUri,
                Constants.GitConfiguration.Credential.SectionName, GitLabConstants.GitConfiguration.Credential.DevOAuthRedirectUri,
                out string redirectUriStr) && Uri.TryCreate(redirectUriStr, UriKind.Absolute, out Uri redirectUri))
            {
                return redirectUri;
            }

            return GitLabConstants.OAuthRedirectUri;
        }

        internal static string GetClientId(ISettings settings)
        {
            // Check for developer override value
            if (settings.TryGetSetting(
                GitLabConstants.EnvironmentVariables.DevOAuthClientId,
                Constants.GitConfiguration.Credential.SectionName, GitLabConstants.GitConfiguration.Credential.DevOAuthClientId,
                out string clientId))
            {
                return clientId;
            }

            return GitLabConstants.OAuthClientId;
        }

        private static string GetClientSecret(ISettings settings)
        {
            // Check for developer override value
            if (settings.TryGetSetting(
                GitLabConstants.EnvironmentVariables.DevOAuthClientSecret,
                Constants.GitConfiguration.Credential.SectionName, GitLabConstants.GitConfiguration.Credential.DevOAuthClientSecret,
                out string clientSecret))
            {
                return clientSecret;
            }

            return GitLabConstants.OAuthClientSecret;
        }
    }
}
