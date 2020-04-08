// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;

namespace Atlassian.Bitbucket
{
    public static class BitbucketConstants
    {
        public const string BitbucketBaseUrlHost = "bitbucket.org";
        public static readonly Uri BitbucketApiUri = new Uri("https://api.bitbucket.org");
        public const string BitbucketAuthHelperName = "Atlassian.Bitbucket.UI";

        public const string OAuth2ClientId = "b5AKdPfpgFdEGpKzPE";
        public const string OAuth2ClientSecret = "7NUP5qUtSR3SxdFK4xAGaU6PMNvNdE59";
        public static readonly Uri OAuth2AuthorizationEndpoint = new Uri("https://bitbucket.org/site/oauth2/authorize");
        public static readonly Uri OAuth2TokenEndpoint = new Uri("https://bitbucket.org/site/oauth2/access_token");
        public static readonly Uri OAuth2RedirectUri = new Uri("http://localhost:46337/");

        public static class OAuthScopes
        {
            public const string RepositoryWrite = "repository:write";
            public const string Account = "account";
        }
    }
}
