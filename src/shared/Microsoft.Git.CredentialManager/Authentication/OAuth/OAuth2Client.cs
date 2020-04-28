// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager.Authentication.OAuth.Json;
using Newtonsoft.Json;

namespace Microsoft.Git.CredentialManager.Authentication.OAuth
{
    /// <summary>
    /// Represents an OAuth2 client application that can perform the basic flows outlined in RFC 6749,
    /// as well as extensions such as OAuth2 Device Authorization Grant (RFC 8628).
    /// </summary>
    public interface IOAuth2Client
    {
        /// <summary>
        /// Retrieve an authorization code grant using a user agent.
        /// </summary>
        /// <param name="scopes">Scopes to request.</param>
        /// <param name="browser">User agent to use to start the authorization code grant flow.</param>
        /// <param name="ct">Token to cancel the operation.</param>
        /// <returns>Authorization code.</returns>
        Task<OAuth2AuthorizationCodeResult> GetAuthorizationCodeAsync(IEnumerable<string> scopes, IOAuth2WebBrowser browser, CancellationToken ct);

        /// <summary>
        /// Retrieve a device code grant.
        /// </summary>
        /// <param name="scopes">Scopes to request.</param>
        /// <param name="ct">Token to cancel the operation.</param>
        /// <exception cref="InvalidOperationException">Thrown if the client has not been configured with a device authorization endpoint.</exception>
        /// <returns>Device code grant result.</returns>
        Task<OAuth2DeviceCodeResult> GetDeviceCodeAsync(IEnumerable<string> scopes, CancellationToken ct);

        /// <summary>
        /// Exchange an authorization code acquired from <see cref="GetAuthorizationCodeAsync"/> for an access token.
        /// </summary>
        /// <param name="authorizationCodeResult">Authorization code grant result.</param>
        /// <param name="ct">Token to cancel the operation.</param>
        /// <returns>Token result.</returns>
        Task<OAuth2TokenResult> GetTokenByAuthorizationCodeAsync(OAuth2AuthorizationCodeResult authorizationCodeResult, CancellationToken ct);

        /// <summary>
        /// Use a refresh token to get a new access token.
        /// </summary>
        /// <param name="refreshToken">Refresh token.</param>
        /// <param name="ct">Token to cancel the operation.</param>
        /// <returns>Token result.</returns>
        Task<OAuth2TokenResult> GetTokenByRefreshTokenAsync(string refreshToken, CancellationToken ct);

        /// <summary>
        /// Exchange a device code grant acquired from <see cref="GetDeviceCodeAsync"/> for an access token.
        /// </summary>
        /// <param name="deviceCodeResult">Device code grant result.</param>
        /// <param name="ct">Token to cancel the operation.</param>
        /// <returns>Token result.</returns>
        Task<OAuth2TokenResult> GetTokenByDeviceCodeAsync(OAuth2DeviceCodeResult deviceCodeResult, CancellationToken ct);
    }

    public class OAuth2Client : IOAuth2Client
    {
        private readonly HttpClient _httpClient;
        private readonly OAuth2ServerEndpoints _endpoints;
        private readonly Uri _redirectUri;
        private readonly string _clientId;
        private readonly string _clientSecret;

        private IOAuth2CodeGenerator _codeGenerator;

        public OAuth2Client(HttpClient httpClient, OAuth2ServerEndpoints endpoints, string clientId, Uri redirectUri = null, string clientSecret = null)
        {
            _httpClient = httpClient;
            _endpoints = endpoints;
            _clientId = clientId;
            _redirectUri = redirectUri;
            _clientSecret = clientSecret;
        }

        public IOAuth2CodeGenerator CodeGenerator
        {
            get => _codeGenerator ?? (_codeGenerator = new OAuth2CryptographicCodeGenerator());
            set => _codeGenerator = value;
        }

        #region IOAuth2Client

        public async Task<OAuth2AuthorizationCodeResult> GetAuthorizationCodeAsync(IEnumerable<string> scopes, IOAuth2WebBrowser browser, CancellationToken ct)
        {
            string state = CodeGenerator.CreateNonce();
            string codeVerifier = CodeGenerator.CreatePkceCodeVerifier();
            string codeChallenge = CodeGenerator.CreatePkceCodeChallenge(OAuth2PkceChallengeMethod.Sha256, codeVerifier);

            var queryParams = new Dictionary<string, string>
            {
                [OAuth2Constants.AuthorizationEndpoint.ResponseTypeParameter] =
                    OAuth2Constants.AuthorizationEndpoint.AuthorizationCodeResponseType,
                [OAuth2Constants.ClientIdParameter] = _clientId,
                [OAuth2Constants.AuthorizationEndpoint.StateParameter] = state,
                [OAuth2Constants.AuthorizationEndpoint.PkceChallengeMethodParameter] =
                    OAuth2Constants.AuthorizationEndpoint.PkceChallengeMethodS256,
                [OAuth2Constants.AuthorizationEndpoint.PkceChallengeParameter] = codeChallenge
            };

            Uri redirectUri = null;
            if (_redirectUri != null)
            {
                redirectUri = browser.UpdateRedirectUri(_redirectUri);
                queryParams[OAuth2Constants.RedirectUriParameter] = redirectUri.ToString();
            }

            string scopesStr = string.Join(" ", scopes);
            if (!string.IsNullOrWhiteSpace(scopesStr))
            {
                queryParams[OAuth2Constants.ScopeParameter] = scopesStr;
            }

            var authorizationUriBuilder = new UriBuilder(_endpoints.AuthorizationEndpoint)
            {
                Query = queryParams.ToQueryString()
            };

            Uri authorizationUri = authorizationUriBuilder.Uri;

            // Open the browser at the request URI to start the authorization code grant flow.
            Uri finalUri = await browser.GetAuthenticationCodeAsync(authorizationUri, redirectUri, ct);

            // Check for errors serious enough we should terminate the flow, such as if the state value returned does
            // not match the one we passed. This indicates a badly implemented Authorization Server, or worse, some
            // form of failed MITM or replay attack.
            IDictionary<string, string> redirectQueryParams = finalUri.GetQueryParameters();
            if (!redirectQueryParams.TryGetValue(OAuth2Constants.AuthorizationGrantResponse.StateParameter, out string replyState))
            {
                throw new OAuth2Exception($"Missing '{OAuth2Constants.AuthorizationGrantResponse.StateParameter}' in response.");
            }
            if (!StringComparer.Ordinal.Equals(state, replyState))
            {
                throw new OAuth2Exception($"Invalid '{OAuth2Constants.AuthorizationGrantResponse.StateParameter}' in response. Does not match initial request.");
            }

            // We expect to have the auth code in the response otherwise terminate the flow (we failed authentication for some reason)
            if (!redirectQueryParams.TryGetValue(OAuth2Constants.AuthorizationGrantResponse.AuthorizationCodeParameter, out string authCode))
            {
                throw new OAuth2Exception($"Missing '{OAuth2Constants.AuthorizationGrantResponse.AuthorizationCodeParameter}' in response.");
            }

            return new OAuth2AuthorizationCodeResult(authCode, redirectUri, codeVerifier);
        }

        public async Task<OAuth2DeviceCodeResult> GetDeviceCodeAsync(IEnumerable<string> scopes, CancellationToken ct)
        {
            if (_endpoints.DeviceAuthorizationEndpoint is null)
            {
                throw new InvalidOperationException("No device authorization endpoint has been configured for this client.");
            }

            string scopesStr = string.Join(" ", scopes);

            var formData = new Dictionary<string, string>
            {
                [OAuth2Constants.ClientIdParameter] = _clientId
            };

            if (!string.IsNullOrWhiteSpace(scopesStr))
            {
                formData[OAuth2Constants.ScopeParameter] = scopesStr;
            }

            using (HttpContent requestContent = new FormUrlEncodedContent(formData))
            using (HttpRequestMessage request = CreateRequestMessage(HttpMethod.Post, _endpoints.DeviceAuthorizationEndpoint, requestContent))
            using (HttpResponseMessage response = await _httpClient.SendAsync(request, ct))
            {
                string json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode && TryDeserializeJson(json, out DeviceAuthorizationEndpointResponseJson jsonObj))
                {
                    return jsonObj.ToResult();
                }

                throw CreateExceptionFromResponse(json);
            }
        }

        public async Task<OAuth2TokenResult> GetTokenByAuthorizationCodeAsync(OAuth2AuthorizationCodeResult authorizationCodeResult, CancellationToken ct)
        {
            var formData = new Dictionary<string, string>
            {
                [OAuth2Constants.TokenEndpoint.GrantTypeParameter] = OAuth2Constants.TokenEndpoint.AuthorizationCodeGrantType,
                [OAuth2Constants.TokenEndpoint.AuthorizationCodeParameter] = authorizationCodeResult.Code,
                [OAuth2Constants.TokenEndpoint.PkceVerifierParameter] = authorizationCodeResult.CodeVerifier,
                [OAuth2Constants.ClientIdParameter] = _clientId
            };

            if (authorizationCodeResult.RedirectUri != null)
            {
                formData[OAuth2Constants.RedirectUriParameter] = authorizationCodeResult.RedirectUri.ToString();
            }

            if (authorizationCodeResult.CodeVerifier != null)
            {
                formData[OAuth2Constants.TokenEndpoint.PkceVerifierParameter] = authorizationCodeResult.CodeVerifier;
            }

            using (HttpContent requestContent = new FormUrlEncodedContent(formData))
            using (HttpRequestMessage request = CreateRequestMessage(HttpMethod.Post, _endpoints.TokenEndpoint, requestContent, true))
            using (HttpResponseMessage response = await _httpClient.SendAsync(request, ct))
            {
                string json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode && TryCreateTokenEndpointResult(json, out OAuth2TokenResult result))
                {
                    return result;
                }

                throw CreateExceptionFromResponse(json);
            }
        }

        public async Task<OAuth2TokenResult> GetTokenByRefreshTokenAsync(string refreshToken, CancellationToken ct)
        {
            var formData = new Dictionary<string, string>
            {
                [OAuth2Constants.TokenEndpoint.GrantTypeParameter] = OAuth2Constants.TokenEndpoint.RefreshTokenGrantType,
                [OAuth2Constants.TokenEndpoint.RefreshTokenParameter] = refreshToken,
                [OAuth2Constants.ClientIdParameter] = _clientId,
            };

            if (_redirectUri != null)
            {
                formData[OAuth2Constants.RedirectUriParameter] = _redirectUri.ToString();
            }

            using (HttpContent requestContent = new FormUrlEncodedContent(formData))
            using (HttpRequestMessage request = CreateRequestMessage(HttpMethod.Post, _endpoints.TokenEndpoint, requestContent, true))
            using (HttpResponseMessage response = await _httpClient.SendAsync(request, ct))
            {
                string json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode && TryCreateTokenEndpointResult(json, out OAuth2TokenResult result))
                {
                    return result;
                }

                throw CreateExceptionFromResponse(json);
            }
        }

        public async Task<OAuth2TokenResult> GetTokenByDeviceCodeAsync(OAuth2DeviceCodeResult deviceCodeResult, CancellationToken ct)
        {
            var formData = new Dictionary<string, string>
            {
                [OAuth2Constants.DeviceAuthorization.GrantTypeParameter] = OAuth2Constants.DeviceAuthorization.DeviceCodeGrantType,
                [OAuth2Constants.DeviceAuthorization.DeviceCodeParameter] = deviceCodeResult.DeviceCode,
                [OAuth2Constants.ClientIdParameter] = _clientId,
            };

            TimeSpan retryInterval = deviceCodeResult.PollingInterval;
            while (true)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    using (HttpContent requestContent = new FormUrlEncodedContent(formData))
                    using (HttpRequestMessage request = CreateRequestMessage(HttpMethod.Post, _endpoints.TokenEndpoint, requestContent))
                    using (HttpResponseMessage response = await _httpClient.SendAsync(request, ct))
                    {
                        string json = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode && TryCreateTokenEndpointResult(json, out OAuth2TokenResult result))
                        {
                            return result;
                        }

                        var error = JsonConvert.DeserializeObject<ErrorResponseJson>(json);

                        switch (error.Error)
                        {
                            case OAuth2Constants.DeviceAuthorization.Errors.AuthorizationPending:
                                // Retry with the current polling interval value
                                break;
                            case OAuth2Constants.DeviceAuthorization.Errors.SlowDown:
                                // We must increase the polling interval by 5 seconds
                                retryInterval = retryInterval.Add(TimeSpan.FromSeconds(5));
                                break;
                            default:
                                // For all other errors do not retry
                                throw CreateExceptionFromResponse(json);
                        }
                    }
                }
                catch (TimeoutException)
                {
                    // Back-off exponentially (2 * x = x + x)
                    retryInterval += retryInterval;
                }

                // Wait the polling interval before retrying
                await Task.Delay(retryInterval, ct);
            }
        }

        #endregion

        #region Extension Points

        protected virtual bool TryCreateTokenEndpointResult(string json, out OAuth2TokenResult result)
        {
            if (TryDeserializeJson(json, out TokenEndpointResponseJson jsonObj))
            {
                result = jsonObj.ToResult();
                return true;
            }

            result = null;
            return false;
        }

        protected virtual bool TryCreateExceptionFromResponse(string json, out OAuth2Exception exception)
        {
            if (TryDeserializeJson(json, out ErrorResponseJson obj))
            {
                exception = obj.ToException();
                return true;
            }

            exception = null;
            return false;
        }

        #endregion

        #region Helpers

        private HttpRequestMessage CreateRequestMessage(HttpMethod method, Uri requestUri, HttpContent content = null, bool addAuthHeader = false)
        {
            var request = new HttpRequestMessage(method, requestUri) {Content = content};
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(Constants.Http.MimeTypeJson));

            if (addAuthHeader && !string.IsNullOrEmpty(_clientSecret))
            {
                request.AddBasicAuthenticationHeader(_clientId, _clientSecret);
            }

            return request;
        }

        private Exception CreateExceptionFromResponse(string json)
        {
            if (TryCreateExceptionFromResponse(json, out OAuth2Exception exception))
            {
                return exception;
            }

            return new OAuth2Exception($"Unknown OAuth error: {json}");
        }

        protected static bool TryDeserializeJson<T>(string json, out T obj)
        {
            try
            {
                obj = JsonConvert.DeserializeObject<T>(json);
                return true;
            }
            catch
            {
                obj = default;
                return false;
            }
        }

        #endregion
    }
}
