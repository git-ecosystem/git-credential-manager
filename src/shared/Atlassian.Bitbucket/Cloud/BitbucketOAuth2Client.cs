// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Net.Http;
using GitCredentialManager;
using GitCredentialManager.Authentication.OAuth;

namespace Atlassian.Bitbucket.Cloud
{
    public class BitbucketOAuth2Client : Bitbucket.BitbucketOAuth2Client
    {
        public BitbucketOAuth2Client(HttpClient httpClient, ISettings settings, ITrace2 trace2)
            : base(httpClient, GetEndpoints(),
                GetClientId(settings), GetRedirectUri(settings), GetClientSecret(settings), trace2)
        {
        }

        public override IEnumerable<string> Scopes => new string[] {
            CloudConstants.OAuthScopes.RepositoryWrite,
            CloudConstants.OAuthScopes.Account,
        };

        private static string GetClientId(ISettings settings)
        {
            // Check for developer override value
            if (settings.TryGetSetting(
                CloudConstants.EnvironmentVariables.OAuthClientId,
                Constants.GitConfiguration.Credential.SectionName, CloudConstants.GitConfiguration.Credential.OAuthClientId,
                out string clientId))
            {
                return clientId;
            }

            return CloudConstants.OAuth2ClientId;
        }

        private static Uri GetRedirectUri(ISettings settings)
        {
            // Check for developer override value
            if (settings.TryGetSetting(
                CloudConstants.EnvironmentVariables.OAuthRedirectUri,
                Constants.GitConfiguration.Credential.SectionName, CloudConstants.GitConfiguration.Credential.OAuthRedirectUri,
                out string redirectUriStr) && Uri.TryCreate(redirectUriStr, UriKind.Absolute, out Uri redirectUri))
            {
                return redirectUri;
            }

            return CloudConstants.OAuth2RedirectUri;
        }

        private static string GetClientSecret(ISettings settings)
        {
            // Check for developer override value
            if (settings.TryGetSetting(
                CloudConstants.EnvironmentVariables.OAuthClientSecret,
                Constants.GitConfiguration.Credential.SectionName, CloudConstants.GitConfiguration.Credential.OAuthClientSecret,
                out string clientSecret))
            {
                return clientSecret;
            }

            return CloudConstants.OAuth2ClientSecret;
        }

        private static OAuth2ServerEndpoints GetEndpoints()
        {
            return new OAuth2ServerEndpoints(
                CloudConstants.OAuth2AuthorizationEndpoint,
                CloudConstants.OAuth2TokenEndpoint
            );
        }
    }
}
