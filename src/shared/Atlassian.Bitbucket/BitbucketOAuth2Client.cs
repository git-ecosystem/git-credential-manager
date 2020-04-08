// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Net.Http;
using Microsoft.Git.CredentialManager.Authentication.OAuth;
using Microsoft.Git.CredentialManager.Authentication.OAuth.Json;
using Newtonsoft.Json;

namespace Atlassian.Bitbucket
{
    public class BitbucketOAuth2Client : OAuth2Client
    {
        private static readonly OAuth2ServerEndpoints Endpoints = new OAuth2ServerEndpoints(
            BitbucketConstants.OAuth2AuthorizationEndpoint,
            BitbucketConstants.OAuth2TokenEndpoint);

        public BitbucketOAuth2Client(HttpClient httpClient)
            : base(httpClient, Endpoints,
                BitbucketConstants.OAuth2ClientId,
                BitbucketConstants.OAuth2RedirectUri,
                BitbucketConstants.OAuth2ClientSecret)
        {
        }

        protected override bool TryCreateTokenEndpointResult(string json, out OAuth2TokenResult result)
        {
            // We override the token endpoint response parsing because the Bitbucket authority returns
            // the non-standard 'scopes' property for the list of scopes, rather than the (optional)
            // 'scope' (note the singular vs plural) property as outlined in the standard.
            if (TryDeserializeJson(json, out BitbucketTokenEndpointResponseJson jsonObj))
            {
                result = jsonObj.ToResult();
                return true;
            }

            result = null;
            return false;
        }

        private class BitbucketTokenEndpointResponseJson : TokenEndpointResponseJson
        {
            // Bitbucket uses "scopes" for the scopes property name rather than the standard "scope" name
            [JsonProperty("scopes")]
            public override string Scope { get; set; }
        }
    }
}
