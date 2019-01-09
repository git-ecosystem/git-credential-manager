// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
namespace GitHub
{
    public static class GitHubConstants
    {
        public const string GitHubBaseUrlHost = "github.com";
        public const string GistBaseUrlHost = "gist." + GitHubBaseUrlHost;

        /// <summary>
        /// The GitHub required HTTP accepts header value
        /// </summary>
        public const string GitHubApiAcceptsHeaderValue = "application/vnd.github.v3+json";
        public const string GitHubOptHeader = "X-GitHub-OTP";

        public static class TokenScopes
        {
            public const string Gist = "gist";
            public const string Repo = "repo";
        }
    }
}
