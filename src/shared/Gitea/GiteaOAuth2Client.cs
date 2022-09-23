using System;
using System.Net.Http;
using GitCredentialManager;
using GitCredentialManager.Authentication.OAuth;

namespace Gitea
{
    public class GiteaOAuth2Client : OAuth2Client
    {
        public GiteaOAuth2Client(HttpClient httpClient, ISettings settings, Uri baseUri)
            : base(httpClient, CreateEndpoints(baseUri),
                GetClientId(settings), GetRedirectUri(settings), GetClientSecret(settings))
        { }

        private static OAuth2ServerEndpoints CreateEndpoints(Uri baseUri)
        {
            Uri authEndpoint = new Uri(baseUri, GiteaConstants.OAuthAuthorizationEndpointRelativeUri);
            Uri tokenEndpoint = new Uri(baseUri, GiteaConstants.OAuthTokenEndpointRelativeUri);

            return new OAuth2ServerEndpoints(authEndpoint, tokenEndpoint);
        }

        private static Uri GetRedirectUri(ISettings settings)
        {
            // Check for developer override value
            if (settings.TryGetSetting(
                GiteaConstants.EnvironmentVariables.DevOAuthRedirectUri,
                Constants.GitConfiguration.Credential.SectionName, GiteaConstants.GitConfiguration.Credential.DevOAuthRedirectUri,
                out string redirectUriStr) && Uri.TryCreate(redirectUriStr, UriKind.Absolute, out Uri redirectUri))
            {
                return redirectUri;
            }

            return GiteaConstants.OAuthRedirectUri;
        }

        internal static string GetClientId(ISettings settings)
        {
            // Check for developer override value
            if (settings.TryGetSetting(
                GiteaConstants.EnvironmentVariables.DevOAuthClientId,
                Constants.GitConfiguration.Credential.SectionName, GiteaConstants.GitConfiguration.Credential.DevOAuthClientId,
                out string clientId))
            {
                return clientId;
            }
            return null;
        }

        private static string GetClientSecret(ISettings settings)
        {
            // Check for developer override value
            if (settings.TryGetSetting(
                GiteaConstants.EnvironmentVariables.DevOAuthClientSecret,
                Constants.GitConfiguration.Credential.SectionName, GiteaConstants.GitConfiguration.Credential.DevOAuthClientSecret,
                out string clientSecret))
            {
                return clientSecret;
            }
            return null;
        }
    }
}
