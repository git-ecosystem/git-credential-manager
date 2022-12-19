using System;

namespace Gitee
{
    public static class GiteeConstants
    {
        public static readonly Uri GiteeDotCom = new Uri("https://gitee.com");

        public const string DefaultAuthenticationHelper = "Gitee.UI";

        // Owned by https://gitee.com/maikebing/git-credential-manager
        public const string OAuthClientId = "09776da975878a86ed655d5ada62a5d4e6faf8e89514ec982794eb61f4092d01";
        public const string OAuthClientSecret = "1d52e7bb3c53b396ad3c4e256ca1eaff27df20a782cd4f1f3dfefa32f44bafc9";

        public static readonly Uri OAuthRedirectUri = new Uri("http://127.0.0.1/");
        // https://gitee.com/api/v5/oauth_doc#/
        public static readonly Uri OAuthAuthorizationEndpointRelativeUri = new Uri("/oauth/authorize", UriKind.Relative);
        public static readonly Uri OAuthTokenEndpointRelativeUri = new Uri("/oauth/token", UriKind.Relative);

        public const AuthenticationModes DotComAuthenticationModes = AuthenticationModes.All;

        public static class EnvironmentVariables
        {
            public const string DevOAuthClientId = "GCM_DEV_GITEE_CLIENTID";
            public const string DevOAuthClientSecret = "GCM_DEV_GITEE_CLIENTSECRET";
            public const string DevOAuthRedirectUri = "GCM_DEV_GITEE_REDIRECTURI";
            public const string AuthenticationModes = "GCM_GITEE_AUTHMODES";
            public const string AuthenticationHelper = "GCM_GITEE_HELPER";
        }

        public static class GitConfiguration
        {
            public static class Credential
            {
                public const string AuthenticationModes = "GiteeAuthModes";
                public const string DevOAuthClientId = "GiteeDevClientId";
                public const string DevOAuthClientSecret = "GiteeDevClientSecret";
                public const string DevOAuthRedirectUri = "GiteeDevRedirectUri";
                public const string AuthenticationHelper = "GiteeHelper";
            }
        }

        public static class HelpUrls
        {
            public const string Gitee = "https://gitee.com/maikebing/git-credential-manager/issues";
        }

        public static bool IsGiteeDotCom(Uri uri) => StringComparer.OrdinalIgnoreCase.Equals(uri.Host, GiteeDotCom.Host);

        public static bool IsGiteeDotComClientId(string clientId) => StringComparer.OrdinalIgnoreCase.Equals(clientId, OAuthClientId);
    }
}
