using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager;
using GitCredentialManager.Authentication.OAuth;

namespace Atlassian.Bitbucket
{
    public abstract class BitbucketOAuth2Client : OAuth2Client
    {
        public BitbucketOAuth2Client(HttpClient httpClient,
            OAuth2ServerEndpoints endpoints,
            string clientId,
            Uri redirectUri,
            string clientSecret,
            ITrace2 trace2) : base(httpClient, endpoints, clientId, trace2, redirectUri, clientSecret, false)
        {
        }

        public abstract IEnumerable<string> Scopes { get; }

        public string GetRefreshTokenServiceName(InputArguments input)
        {
            Uri baseUri = input.GetRemoteUri(includeUser: false);

            // The refresh token key never includes the path component.
            // Instead we use the path component to specify this is the "refresh_token".
            Uri uri = new UriBuilder(baseUri) { Path = "/refresh_token" }.Uri;

            return uri.AbsoluteUri.TrimEnd('/');
        }

        public Task<OAuth2AuthorizationCodeResult> GetAuthorizationCodeAsync(IOAuth2WebBrowser browser, CancellationToken ct)
        {
            return this.GetAuthorizationCodeAsync(Scopes, browser, ct);
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
    }
}
