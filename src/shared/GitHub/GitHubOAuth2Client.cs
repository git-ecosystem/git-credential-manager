using System;
using System.Net.Http;
using Microsoft.Git.CredentialManager.Authentication.OAuth;

namespace GitHub
{
    public class GitHubOAuth2Client : OAuth2Client
    {
        private static readonly string ClientId = "0120e057bd645470c1ed";
        private static readonly string ClientSecret = "18867509d956965542b521a529a79bb883344c90";
        private static readonly Uri RedirectUri = new Uri("http://localhost/");

        public GitHubOAuth2Client(HttpClient httpClient, Uri baseUri)
            : base(httpClient, CreateEndpoints(baseUri), ClientId, RedirectUri, ClientSecret) { }

        private static OAuth2ServerEndpoints CreateEndpoints(Uri baseUri)
        {
            Uri authEndpoint = new Uri(baseUri, "/login/oauth/authorize");
            Uri tokenEndpoint = new Uri(baseUri, "/login/oauth/access_token");

            Uri deviceAuthEndpoint = null;
            if (GitHubConstants.IsOAuthDeviceAuthSupported)
            {
                deviceAuthEndpoint = new Uri(baseUri, "/login/oauth/authorize/device");
            }

            return new OAuth2ServerEndpoints(authEndpoint, tokenEndpoint)
            {
                DeviceAuthorizationEndpoint = deviceAuthEndpoint
            };
        }
    }
}
