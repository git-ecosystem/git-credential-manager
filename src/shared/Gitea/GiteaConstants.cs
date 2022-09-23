using System;

namespace Gitea
{
    public static class GiteaConstants
    {
        public static readonly Uri OAuthRedirectUri = new Uri("http://127.0.0.1/");
        // https://docs.gitea.io/en-us/oauth2-provider/
        public static readonly Uri OAuthAuthorizationEndpointRelativeUri = new Uri("/login/oauth/authorize", UriKind.Relative);
        public static readonly Uri OAuthTokenEndpointRelativeUri = new Uri("/login/oauth/access_token", UriKind.Relative);

        public const AuthenticationModes DotComAuthenticationModes = AuthenticationModes.All;

        public static class EnvironmentVariables
        {
            public const string DevOAuthClientId = "GCM_DEV_GITEA_CLIENTID";
            public const string DevOAuthClientSecret = "GCM_DEV_GITEA_CLIENTSECRET";
            public const string DevOAuthRedirectUri = "GCM_DEV_GITEA_REDIRECTURI";
            public const string AuthenticationModes = "GCM_GITEA_AUTHMODES";
            public const string AuthenticationHelper = "GCM_GITEA_HELPER";
        }

        public static class GitConfiguration
        {
            public static class Credential
            {
                public const string AuthenticationModes = "giteaAuthModes";
                public const string DevOAuthClientId = "giteaDevClientId";
                public const string DevOAuthClientSecret = "giteaDevClientSecret";
                public const string DevOAuthRedirectUri = "giteaDevRedirectUri";
                public const string AuthenticationHelper = "giteaHelper";
            }
        }

        public static class HelpUrls
        {
            public const string Gitea = "https://github.com/GitCredentialManager/git-credential-manager/blob/main/docs/gitea.md";
        }
    }
}
