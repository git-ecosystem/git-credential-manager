using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GitCredentialManager.Authentication.OAuth
{
    public interface IOAuth2WebBrowser
    {
        Uri UpdateRedirectUri(Uri uri);

        /// <summary>
        /// Drive the user agent through the authorization request and intercept the
        /// authorization response delivered to the redirect URI.
        /// </summary>
        /// <param name="authorizationUri">Authorization request URI to open in the user agent.</param>
        /// <param name="redirectUri">Redirect URI to intercept the response on.</param>
        /// <param name="responseMode">Mechanism the authorization server uses to deliver the response.</param>
        /// <param name="ct">Token to cancel the operation.</param>
        /// <returns>The authorization response parameters.</returns>
        Task<IDictionary<string, string>> GetAuthenticationResponseAsync(
            Uri authorizationUri, Uri redirectUri, OAuth2ResponseMode responseMode, CancellationToken ct);
    }
}
