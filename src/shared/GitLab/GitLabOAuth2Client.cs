using System;
using System.Net.Http;
using GitCredentialManager;
using GitCredentialManager.Authentication.OAuth;

namespace GitLab
{
    public class GitLabOAuth2Client : OAuth2Client
    {
        public GitLabOAuth2Client(HttpClient httpClient, ISettings settings, Uri baseUri)
            : base(httpClient, CreateEndpoints(baseUri),
                GetClientId(settings, baseUri), GetRedirectUri(settings), GetClientSecret(settings, baseUri))
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

        internal static string GetClientId(ISettings settings, Uri baseUri)
        {
            // Check for developer override value
            if (settings.TryGetSetting(
                GitLabConstants.EnvironmentVariables.DevOAuthClientId,
                Constants.GitConfiguration.Credential.SectionName, GitLabConstants.GitConfiguration.Credential.DevOAuthClientId,
                out string clientId))
            {
                return clientId;
            }

            GitLabApplication instance;
            if (GitLabConstants.GitLabApplicationsByHost.TryGetValue(baseUri.Host, out instance))
            {
                return instance.OAuthClientId;
            }
            throw new ArgumentException($"Missing OAuth configuration for {baseUri.Host}, see https://github.com/GitCredentialManager/git-credential-manager/blob/main/docs/gitlab.md.");
        }

        private static string GetClientSecret(ISettings settings, Uri baseUri)
        {
            // Check for developer override value
            if (settings.TryGetSetting(
                GitLabConstants.EnvironmentVariables.DevOAuthClientSecret,
                Constants.GitConfiguration.Credential.SectionName, GitLabConstants.GitConfiguration.Credential.DevOAuthClientSecret,
                out string clientSecret))
            {
                return clientSecret;
            }

            GitLabApplication instance;
            if (GitLabConstants.GitLabApplicationsByHost.TryGetValue(baseUri.Host, out instance))
            {
                return instance.OAuthClientSecret;
            }
            throw new ArgumentException($"Missing OAuth configuration for {baseUri.Host}, see https://github.com/GitCredentialManager/git-credential-manager/blob/main/docs/gitlab.md.");
        }
    }
}
