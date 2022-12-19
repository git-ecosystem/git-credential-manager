using System;
using System.Net.Http;
using GitCredentialManager;
using GitCredentialManager.Authentication.OAuth;

namespace Gitee
{
    public class GiteeOAuth2Client : OAuth2Client
    {
        public GiteeOAuth2Client(HttpClient httpClient, ISettings settings, Uri baseUri)
            : base(httpClient, CreateEndpoints(baseUri),
                GetClientId(settings), GetRedirectUri(settings), GetClientSecret(settings))
        { }

        private static OAuth2ServerEndpoints CreateEndpoints(Uri baseUri)
        {
            Uri authEndpoint = new Uri(baseUri, GiteeConstants.OAuthAuthorizationEndpointRelativeUri);
            Uri tokenEndpoint = new Uri(baseUri, GiteeConstants.OAuthTokenEndpointRelativeUri);

            return new OAuth2ServerEndpoints(authEndpoint, tokenEndpoint);
        }

        private static Uri GetRedirectUri(ISettings settings)
        {
            // Check for developer override value
            if (settings.TryGetSetting(
                GiteeConstants.EnvironmentVariables.DevOAuthRedirectUri,
                Constants.GitConfiguration.Credential.SectionName, GiteeConstants.GitConfiguration.Credential.DevOAuthRedirectUri,
                out string redirectUriStr) && Uri.TryCreate(redirectUriStr, UriKind.Absolute, out Uri redirectUri))
            {
                return redirectUri;
            }

            return GiteeConstants.OAuthRedirectUri;
        }

        internal static string GetClientId(ISettings settings)
        {
            // Check for developer override value
            if (settings.TryGetSetting(
                GiteeConstants.EnvironmentVariables.DevOAuthClientId,
                Constants.GitConfiguration.Credential.SectionName, GiteeConstants.GitConfiguration.Credential.DevOAuthClientId,
                out string clientId))
            {
                return clientId;
            }

            return GiteeConstants.OAuthClientId;
        }

        private static string GetClientSecret(ISettings settings)
        {
            // Check for developer override value
            if (settings.TryGetSetting(
                GiteeConstants.EnvironmentVariables.DevOAuthClientSecret,
                Constants.GitConfiguration.Credential.SectionName, GiteeConstants.GitConfiguration.Credential.DevOAuthClientSecret,
                out string clientSecret))
            {
                return clientSecret;
            }

            return GiteeConstants.OAuthClientSecret;
        }
    }
}
