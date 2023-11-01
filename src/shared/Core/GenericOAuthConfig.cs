using System;
using System.Collections.Generic;
using System.Linq;
using GitCredentialManager.Authentication.OAuth;

namespace GitCredentialManager
{
    public class GenericOAuthConfig
    {
        public static bool TryGet(ITrace trace, ISettings settings, InputArguments input, out GenericOAuthConfig config)
        {
            config = new GenericOAuthConfig();
            Uri authzEndpointUri = null;
            Uri tokenEndpointUri = null;
            var remoteUri = input.GetRemoteUri();

            if (input.WwwAuth.Any(x => x.Contains("Basic realm=\"Gitea\"", StringComparison.OrdinalIgnoreCase)))
            {
                trace.WriteLine($"Using universal Gitea OAuth configuration");
                // https://docs.gitea.com/next/development/oauth2-provider?_highlight=oauth#pre-configured-applications
                config.ClientId = WellKnown.GiteaClientId;
                authzEndpointUri = new Uri(remoteUri, WellKnown.GiteaAuthzEndpoint);
                tokenEndpointUri = new Uri(remoteUri, WellKnown.GiteaTokenEndpoint);
                config.RedirectUri = WellKnown.LocalIPv4RedirectUri;
            }

            if (settings.TryGetSetting(
                    Constants.EnvironmentVariables.OAuthAuthzEndpoint,
                    Constants.GitConfiguration.Credential.SectionName,
                    Constants.GitConfiguration.Credential.OAuthAuthzEndpoint,
                    out string authzEndpoint))
            {
                Uri.TryCreate(remoteUri, authzEndpoint, out authzEndpointUri);
            }

            if (authzEndpointUri == null)
            {
                trace.WriteLine($"Invalid OAuth configuration - missing/invalid authorize endpoint: {authzEndpoint}");
                config = null;
                return false;
            }

            if (settings.TryGetSetting(
                    Constants.EnvironmentVariables.OAuthTokenEndpoint,
                    Constants.GitConfiguration.Credential.SectionName,
                    Constants.GitConfiguration.Credential.OAuthTokenEndpoint,
                    out string tokenEndpoint))
            {
                Uri.TryCreate(remoteUri, tokenEndpoint, out tokenEndpointUri);
            }
            
            if (tokenEndpointUri == null)
            {
                trace.WriteLine($"Invalid OAuth configuration - missing/invalid token endpoint: {tokenEndpoint}");
                config = null;
                return false;
            }

            // Device code endpoint is optional
            Uri deviceEndpointUri = null;
            if (settings.TryGetSetting(
                    Constants.EnvironmentVariables.OAuthDeviceEndpoint,
                    Constants.GitConfiguration.Credential.SectionName,
                    Constants.GitConfiguration.Credential.OAuthDeviceEndpoint,
                    out string deviceEndpoint))
            {
                if (!Uri.TryCreate(remoteUri, deviceEndpoint, out deviceEndpointUri))
                {
                    trace.WriteLine($"Invalid OAuth configuration - invalid device endpoint: {deviceEndpoint}");
                }
            }

            config.Endpoints = new OAuth2ServerEndpoints(authzEndpointUri, tokenEndpointUri)
            {
                DeviceAuthorizationEndpoint = deviceEndpointUri
            };

            if (settings.TryGetSetting(
                    Constants.EnvironmentVariables.OAuthClientId,
                    Constants.GitConfiguration.Credential.SectionName,
                    Constants.GitConfiguration.Credential.OAuthClientId,
                    out string clientId))
            {
                config.ClientId = clientId;
            }

            if (settings.TryGetSetting(
                    Constants.EnvironmentVariables.OAuthClientSecret,
                    Constants.GitConfiguration.Credential.SectionName,
                    Constants.GitConfiguration.Credential.OAuthClientSecret,
                    out string clientSecret))
            {
                config.ClientSecret = clientSecret;
            }

            if (settings.TryGetSetting(
                    Constants.EnvironmentVariables.OAuthRedirectUri,
                    Constants.GitConfiguration.Credential.SectionName,
                    Constants.GitConfiguration.Credential.OAuthRedirectUri,
                    out string redirectUrl) && Uri.TryCreate(redirectUrl, UriKind.Absolute, out Uri redirectUri))
            {
                config.RedirectUri = redirectUri;
            }

            if (config.RedirectUri == null)
            {
                config.RedirectUri = new Uri("http://127.0.0.1");
            }

            if (settings.TryGetSetting(
                    Constants.EnvironmentVariables.OAuthScopes,
                    Constants.GitConfiguration.Credential.SectionName,
                    Constants.GitConfiguration.Credential.OAuthScopes,
                    out string scopesStr) && !string.IsNullOrWhiteSpace(scopesStr))
            {
                config.Scopes = scopesStr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                config.Scopes = Array.Empty<string>();
            }

            if (settings.TryGetSetting(
                    Constants.EnvironmentVariables.OAuthClientAuthHeader,
                    Constants.GitConfiguration.Credential.SectionName,
                    Constants.GitConfiguration.Credential.OAuthClientAuthHeader,
                    out string useHeader))
            {
                config.UseAuthHeader = useHeader.IsTruthy();
            }
            else
            {
                // Default to true
                config.UseAuthHeader = true;
            }

            config.DefaultUserName = settings.TryGetSetting(
                Constants.EnvironmentVariables.OAuthDefaultUserName,
                Constants.GitConfiguration.Credential.SectionName,
                Constants.GitConfiguration.Credential.OAuthDefaultUserName,
                out string userName)
                ? userName
                : "OAUTH_USER";

            return true;
        }


        public OAuth2ServerEndpoints Endpoints { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public Uri RedirectUri { get; set; }
        public string[] Scopes { get; set; }
        public bool UseAuthHeader { get; set; }
        public string DefaultUserName { get; set; }

        public bool SupportsDeviceCode => Endpoints.DeviceAuthorizationEndpoint != null;

    public static class WellKnown
    {
        // https://docs.gitea.com/next/development/oauth2-provider?_highlight=oauth#pre-configured-applications
        public const string GiteaClientId = "e90ee53c-94e2-48ac-9358-a874fb9e0662";
        // https://docs.gitea.com/next/development/oauth2-provider?_highlight=oauth#endpoints
        public const string GiteaAuthzEndpoint = "/login/oauth/authorize";
        public const string GiteaTokenEndpoint = "/login/oauth/access_token";
        public static Uri LocalIPv4RedirectUri = new Uri("http://127.0.0.1");
    }
    }
}
