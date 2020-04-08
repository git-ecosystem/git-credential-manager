// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;

namespace GitHub
{
    public static class GitHubConstants
    {
        public const string GitHubBaseUrlHost = "github.com";
        public const string GistBaseUrlHost = "gist." + GitHubBaseUrlHost;

        public const string AuthHelperName = "GitHub.Authentication.Helper";

        /// <summary>
        /// The GitHub required HTTP accepts header value
        /// </summary>
        public const string GitHubApiAcceptsHeaderValue = "application/vnd.github.v3+json";
        public const string GitHubOptHeader = "X-GitHub-OTP";

        /// <summary>
        /// Minimum GitHub Enterprise version that supports OAuth authentication with GCM Core.
        /// </summary>
        // TODO: update this with a real version number once the GCM OAuth application has been deployed to GHE
        public static readonly Version MinimumEnterpriseOAuthVersion = new Version("99.99.99");

        /// <summary>
        /// Supported authentication modes for GitHub.com.
        /// </summary>
        // TODO: remove Basic once the GCM OAuth app is whitelisted and does not require installation in every organization
        public const AuthenticationModes DotDomAuthenticationModes = AuthenticationModes.Basic | AuthenticationModes.OAuth;

        /// <summary>
        /// Check if RFC 8628 is supported by GitHub.com and GHE.
        /// </summary>
        // TODO: remove this once device auth is supported
        public const bool IsOAuthDeviceAuthSupported = false;

        public static class TokenScopes
        {
            public const string Gist = "gist";
            public const string Repo = "repo";
        }

        public static class OAuthScopes
        {
            public const string Gist = "gist";
            public const string Repo = "repo";
            public const string Workflow = "workflow";
        }

        public static class EnvironmentVariables
        {
            public const string AuthenticationModes = "GCM_GITHUB_AUTHMODES";
        }

        public static class GitConfiguration
        {
            public static class Credential
            {
                public const string AuthModes = "gitHubAuthModes";
            }
        }
    }
}
