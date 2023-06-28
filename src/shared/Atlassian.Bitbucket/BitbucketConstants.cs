using System;

namespace Atlassian.Bitbucket
{
    public static class BitbucketConstants
    {
        public const string Id = "bitbucket";

        public const string Name = "Bitbucket";

        public const string DefaultAuthenticationHelper = "Atlassian.Bitbucket.UI";

        public static class EnvironmentVariables
        {
            public const string AuthenticationHelper = "GCM_BITBUCKET_HELPER";
            public const string AuthenticationModes = "GCM_BITBUCKET_AUTHMODES";
            public const string AlwaysRefreshCredentials = "GCM_BITBUCKET_ALWAYS_REFRESH_CREDENTIALS";
            public const String ValidateStoredCredentials = "GCM_BITBUCKET_VALIDATE_STORED_CREDENTIALS";
        }

        public static class GitConfiguration
        {
            public static class Credential
            {
                public const string AuthenticationHelper = "bitbucketHelper";
                public const string AuthenticationModes = "bitbucketAuthModes";
                public const string AlwaysRefreshCredentials = "bitbucketAlwaysRefreshCredentials";
                public const string ValidateStoredCredentials = "bitbucketValidateStoredCredentials";
            }
        }
        public static class HelpUrls
        {
            public const string DataCenterPasswordReset = "/passwordreset";
            public const string DataCenterLogin = "/login";
            public const string PasswordReset = "https://bitbucket.org/account/password/reset/";
            public const string SignUp = "https://bitbucket.org/account/signup/";
            public const string TwoFactor = "https://support.atlassian.com/bitbucket-cloud/docs/enable-two-step-verification/";
        }

    }
}
