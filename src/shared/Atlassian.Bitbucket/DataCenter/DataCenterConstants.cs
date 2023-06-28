using System;

namespace Atlassian.Bitbucket.DataCenter
{
    public static class DataCenterConstants
    {
        public static class OAuthScopes
        {
            public const string PublicRepos = "PUBLIC_REPOS";
            public const string RepoWrite = "REPO_WRITE";
            public const string RepoRead = "REPO_READ";
        }

        public static readonly Uri OAuth2RedirectUri = new Uri("http://localhost:34106/");

        /// <summary>
        /// Supported authentication modes for Bitbucket Server/DC
        /// </summary>
        public const AuthenticationModes ServerAuthenticationModes = AuthenticationModes.Basic  | AuthenticationModes.OAuth;

        /// <summary>
        /// Bitbucket Server/DC does not have a REST API we can use to trade an OAuth access_token for the owning username.
        /// However one is needed to construct the Basic Auth request made by Git HTTP requests, therefore use a hardcoded
        /// placeholder for the username.
        /// </summary>
        public const string OAuthUserName = "OAUTH_USERNAME";

        public static class EnvironmentVariables
        {
            public const string OAuthClientId = "GCM_BITBUCKET_DATACENTER_CLIENTID";
            public const string OAuthClientSecret = "GCM_BITBUCKET_DATACENTER_CLIENTSECRET";
            public const string OAuthRedirectUri = "GCM_BITBUCKET_DATACENTER_OAUTH_REDIRECTURI";
        }

        public static class GitConfiguration
        {
            public static class Credential
            {
                public const string OAuthClientId = "bitbucketDataCenterOAuthClientId";
                public const string OAuthClientSecret = "bitbucketDataCenterOAuthClientSecret";
                public const string OAuthRedirectUri = "bitbucketDataCenterOauthRedirectUri";
            }
        }
    }
}
