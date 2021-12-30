using System;
using System.Collections.Generic;
using System.Linq;

namespace GitLab
{
    public record GitLabApplication
    {
        public GitLabApplication(string host, string oAuthClientId, string oAuthClientSecret)
        {
            Host = host;
            OAuthClientId = oAuthClientId;
            OAuthClientSecret = oAuthClientSecret;
        }

        public string Host { get; }
        public string OAuthClientId { get; }
        public string OAuthClientSecret { get; }
    }


    public static class GitLabConstants
    {
        // To add an instance, follow https://docs.gitlab.com/ee/integration/oauth_provider.html
        // specify callback/redirect URI http://127.0.0.1/ and check 'confidential', 'expire access tokens', 'write_repository'
        private static readonly GitLabApplication[] GitLabApplications =
        {
            new GitLabApplication(host: "gitlab.com",
                // https://gitlab.com/oauth/applications/207177/edit owned by hickford
                oAuthClientId: "d8a14250a4d1beacaad67dd6fabaab1e0408b581ca73ae4a76cc7170d3f8afd1",
                oAuthClientSecret : "58b5f5e0c99a5be9ac13f4ba15992cc72c5594386e82aecac94da964147d3151"
            ),
            new GitLabApplication(host: "gitlab.freedesktop.org",
                // https://gitlab.freedesktop.org/oauth/applications/52 owned by hickford
                oAuthClientId: "6503d8c5a27187628440d44e0352833a2b49bce540c546c22a3378c8f5b74d45",
                oAuthClientSecret: "2ae9343a034ff1baadaef1e7ce3197776b00746a02ddf0323bb34aca8bff6dc1")
        };
        public static readonly IDictionary<string, GitLabApplication> GitLabApplicationsByHost = GitLabApplications.ToDictionary(x => x.Host, StringComparer.InvariantCultureIgnoreCase);

        public static readonly Uri OAuthRedirectUri = new Uri("http://127.0.0.1/");
        // https://docs.gitlab.com/ee/api/oauth2.html#authorization-code-flow
        public static readonly Uri OAuthAuthorizationEndpointRelativeUri = new Uri("/oauth/authorize", UriKind.Relative);
        public static readonly Uri OAuthTokenEndpointRelativeUri = new Uri("/oauth/token", UriKind.Relative);

        public const AuthenticationModes DotComAuthenticationModes = AuthenticationModes.Browser | AuthenticationModes.Pat;

        public static class EnvironmentVariables
        {
            public const string DevOAuthClientId = "GCM_DEV_GITLAB_CLIENTID";
            public const string DevOAuthClientSecret = "GCM_DEV_GITLAB_CLIENTSECRET";
            public const string DevOAuthRedirectUri = "GCM_DEV_GITLAB_REDIRECTURI";
            public const string AuthenticationModes = "GCM_GITLAB_AUTHMODES";

        }

        public static class GitConfiguration
        {
            public static class Credential
            {
                public const string AuthenticationModes = "GitLabAuthModes";
                public const string DevOAuthClientId = "GitLabDevClientId";
                public const string DevOAuthClientSecret = "GitLabDevClientSecret";
                public const string DevOAuthRedirectUri = "GitLabDevRedirectUri";
            }
        }
    }
}
