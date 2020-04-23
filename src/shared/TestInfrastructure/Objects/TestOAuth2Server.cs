// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager.Authentication.OAuth;
using Microsoft.Git.CredentialManager.Authentication.OAuth.Json;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests.Objects
{
    public class TestOAuth2Server
    {
        private readonly IDictionary<string, OAuth2Application> _apps = new Dictionary<string, OAuth2Application>();
        private readonly Uri _deviceCodeVerificationUri = new Uri("https://example.com/devicelogin");

        public TestOAuth2Server(OAuth2ServerEndpoints endpoints)
        {
            Endpoints = endpoints;
        }

        public OAuth2ServerEndpoints Endpoints { get; }

        public TestOAuth2ServerTokenGenerator TokenGenerator = new TestOAuth2ServerTokenGenerator();

        public void RegisterApplication(OAuth2Application application)
        {
            _apps[application.Id] = application;
        }

        public void Bind(TestHttpMessageHandler httpHandler)
        {
            httpHandler.Setup(HttpMethod.Get, Endpoints.AuthorizationEndpoint, OnAuthorizationEndpointAsync);
            httpHandler.Setup(HttpMethod.Post, Endpoints.DeviceAuthorizationEndpoint, OnDeviceAuthorizationEndpointAsync);
            httpHandler.Setup(HttpMethod.Post, Endpoints.TokenEndpoint, OnTokenEndpointAsync);
        }

        public void SignInDeviceWithUserCode(string userCode)
        {
            OAuth2Application app = _apps.Values.FirstOrDefault(x => x.OwnsDeviceCodeGrant(userCode));
            if (app is null)
            {
                throw new Exception($"Unknown user code '{userCode}'");
            }

            app.ApproveDeviceCodeGrant(userCode);
        }

        private Task<HttpResponseMessage> OnAuthorizationEndpointAsync(HttpRequestMessage request)
        {
            IDictionary<string, string> reqQuery = request.RequestUri.GetQueryParameters();

            // The only support response type so far is 'code'
            Assert.True(reqQuery.TryGetValue(OAuth2Constants.AuthorizationEndpoint.ResponseTypeParameter, out string respType));
            Assert.Equal(OAuth2Constants.AuthorizationEndpoint.AuthorizationCodeResponseType, respType);

            // The client/app ID must be specified and must match a known application
            if (!reqQuery.TryGetValue(OAuth2Constants.ClientIdParameter, out string clientId) ||
                !_apps.TryGetValue(clientId, out OAuth2Application app))
            {
                throw new Exception($"Unknown OAuth application '{clientId}'");
            }

            // Redirect is optional, but if it is specified it must match a registered URI
            reqQuery.TryGetValue(OAuth2Constants.RedirectUriParameter, out string redirectUriStr);
            Uri redirectUri = app.ValidateRedirect(redirectUriStr);

            // Scope is optional
            reqQuery.TryGetValue(OAuth2Constants.ScopeParameter, out string scopeStr);
            string[] scopes = scopeStr?.Split(' ');

            // Code challenge is optional
            reqQuery.TryGetValue(OAuth2Constants.AuthorizationEndpoint.PkceChallengeParameter, out string codeChallenge);

            // Code challenge method is optional and defaults to "plain"
            var codeChallengeMethod = OAuth2PkceChallengeMethod.Plain;
            if (reqQuery.TryGetValue(OAuth2Constants.AuthorizationEndpoint.PkceChallengeMethodParameter, out string challengeMethodStr))
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(challengeMethodStr,
                    OAuth2Constants.AuthorizationEndpoint.PkceChallengeMethodPlain))
                {
                    codeChallengeMethod = OAuth2PkceChallengeMethod.Plain;
                }
                else if (StringComparer.OrdinalIgnoreCase.Equals(challengeMethodStr,
                    OAuth2Constants.AuthorizationEndpoint.PkceChallengeMethodS256))
                {
                    codeChallengeMethod = OAuth2PkceChallengeMethod.Sha256;
                }
                else
                {
                    throw new Exception($"Unsupported code challenge method '{challengeMethodStr}'");
                }
            }

            // Create the auth code grant
            OAuth2Application.AuthCodeGrant grant = app.CreateAuthorizationCodeGrant(
                TokenGenerator, scopes, redirectUriStr, codeChallenge, codeChallengeMethod);

            var respQuery = new Dictionary<string, string>
            {
                [OAuth2Constants.AuthorizationGrantResponse.AuthorizationCodeParameter] = grant.Code
            };

            // State is optional but must be included in the reply if specified
            if (reqQuery.TryGetValue(OAuth2Constants.AuthorizationEndpoint.StateParameter, out string state))
            {
                respQuery[OAuth2Constants.AuthorizationGrantResponse.StateParameter] = state;
            }

            // Build the final redirect URI including the auth code
            var ub = new UriBuilder(redirectUri)
            {
                Query = respQuery.ToQueryString()
            };

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request,
                Headers = {Location = ub.Uri}
            };

            return Task.FromResult(response);
        }

        private async Task<HttpResponseMessage> OnDeviceAuthorizationEndpointAsync(HttpRequestMessage request)
        {
            IDictionary<string, string> formData = await request.Content.ReadAsFormContentAsync();

            // The client/app ID must be specified and must match a known application
            if (!formData.TryGetValue(OAuth2Constants.ClientIdParameter, out string clientId) ||
                !_apps.TryGetValue(clientId, out OAuth2Application app))
            {
                throw new Exception($"Unknown OAuth application '{clientId}'");
            }

            // Scope is optional
            formData.TryGetValue(OAuth2Constants.ScopeParameter, out string scopeStr);
            string[] scopes = scopeStr?.Split(' ');

            // Create the device code grant
            OAuth2Application.DeviceCodeGrant grant = app.CreateDeviceCodeGrant(TokenGenerator, scopes);

            var deviceResp = new DeviceAuthorizationEndpointResponseJson
            {
                DeviceCode = grant.DeviceCode,
                UserCode = grant.UserCode,
                VerificationUri = _deviceCodeVerificationUri,
            };

            string responseJson = JsonConvert.SerializeObject(deviceResp);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request,
                Content = new StringContent(responseJson)
            };
        }

        private async Task<HttpResponseMessage> OnTokenEndpointAsync(HttpRequestMessage request)
        {
            IDictionary<string, string> formData = await request.Content.ReadAsFormContentAsync();

            if (!formData.TryGetValue(OAuth2Constants.TokenEndpoint.GrantTypeParameter, out string grantType))
            {
                throw new Exception("Missing grant type");
            }

            if (!formData.TryGetValue(OAuth2Constants.ClientIdParameter, out string clientId))
            {
                throw new Exception("Missing client ID in request body");
            }

            if (!_apps.TryGetValue(clientId, out OAuth2Application app))
            {
                throw new Exception($"Unknown OAuth application '{clientId}'");
            }

            TokenEndpointResponseJson tokenResp;
            if (StringComparer.OrdinalIgnoreCase.Equals(grantType, OAuth2Constants.TokenEndpoint.AuthorizationCodeGrantType))
            {
                if (!formData.TryGetValue(OAuth2Constants.TokenEndpoint.AuthorizationCodeParameter, out string authCode))
                {
                    throw new Exception("Missing authorization code parameter");
                }

                formData.TryGetValue(OAuth2Constants.TokenEndpoint.PkceVerifierParameter, out string codeVerifier);
                if (formData.TryGetValue(OAuth2Constants.RedirectUriParameter, out string redirectUriStr))
                {
                    app.ValidateRedirect(redirectUriStr);
                }

                tokenResp = app.CreateTokenByAuthorizationGrant(TokenGenerator, authCode, codeVerifier, redirectUriStr);
            }
            else if (StringComparer.OrdinalIgnoreCase.Equals(grantType, OAuth2Constants.TokenEndpoint.RefreshTokenGrantType))
            {
                if (!formData.TryGetValue(OAuth2Constants.TokenEndpoint.RefreshTokenParameter, out string refreshToken))
                {
                    throw new Exception("Missing refresh token parameter");
                }

                app.ValidateRedirect(formData[OAuth2Constants.RedirectUriParameter]);

                tokenResp = app.CreateTokenByRefreshTokenGrant(TokenGenerator, refreshToken);
            }
            else if (StringComparer.OrdinalIgnoreCase.Equals(grantType, OAuth2Constants.DeviceAuthorization.DeviceCodeGrantType))
            {
                if (!formData.TryGetValue(OAuth2Constants.DeviceAuthorization.DeviceCodeParameter, out string deviceCode))
                {
                    throw new Exception("Missing device code parameter");
                }

                if (app.IsDeviceCodeGrantApproved(deviceCode))
                {
                    tokenResp = app.CreateTokenByDeviceCodeGrant(TokenGenerator, deviceCode);
                }
                else
                {
                    var errorResp = new ErrorResponseJson
                    {
                        Error = OAuth2Constants.DeviceAuthorization.Errors.AuthorizationPending
                    };

                    var errorJson = JsonConvert.SerializeObject(errorResp);

                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        RequestMessage = request,
                        Content = new StringContent(errorJson)
                    };
                }
            }
            else
            {
                throw new Exception($"Unknown grant type '{grantType}'");
            }

            string responseJson = JsonConvert.SerializeObject(tokenResp);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request,
                Content = new StringContent(responseJson)
            };
        }
    }

    public class TestOAuth2ServerTokenGenerator
    {
        private int _authCodesIndex = -1;
        private int _deviceCodesIndex = -1;
        private int _userCodesIndex = -1;
        private int _accessTokensIndex = -1;
        private int _refreshTokensIndex = -1;

        public readonly List<string> AuthCodes = new List<string>();
        public readonly List<string> DeviceCodes = new List<string>();
        public readonly List<string> UserCodes = new List<string>();
        public readonly List<string> AccessTokens = new List<string>();
        public readonly List<string> RefreshTokens = new List<string>();

        public string CreateAuthorizationCode() => GetNextValueOrRandom(AuthCodes, ref _authCodesIndex);
        public string CreateDeviceCode() => GetNextValueOrRandom(DeviceCodes, ref _deviceCodesIndex);
        public string CreateUserCode() => GetNextValueOrRandom(UserCodes, ref _userCodesIndex);
        public string CreateAccessToken() => GetNextValueOrRandom(AccessTokens, ref _accessTokensIndex);
        public string CreateRefreshToken() => GetNextValueOrRandom(RefreshTokens, ref _refreshTokensIndex);

        private static string GetNextValueOrRandom(List<string> values, ref int index)
        {
            if (index < 0)
            {
                index = 0;
            }

            if (index < values.Count)
            {
                return values[index++];
            }

            return Guid.NewGuid().ToString("N").Substring(8);
        }
    }

    public class OAuth2Application
    {
        public class AuthCodeGrant
        {
            public AuthCodeGrant(string code, string[] scopes, string redirectUri = null,
                string codeChallenge = null, OAuth2PkceChallengeMethod codeChallengeMethod = OAuth2PkceChallengeMethod.Plain)
            {
                Code = code;
                Scopes = scopes;
                RedirectUri = redirectUri;
                CodeChallenge = codeChallenge;
                CodeChallengeMethod = codeChallengeMethod;
            }
            public string Code { get; }
            public string[] Scopes { get; }
            public string RedirectUri { get; }
            public string CodeChallenge { get; }
            public OAuth2PkceChallengeMethod CodeChallengeMethod { get; }
        }

        public class DeviceCodeGrant
        {
            public DeviceCodeGrant(string userCode, string deviceCode, string[] scopes)
            {
                UserCode = userCode;
                DeviceCode = deviceCode;
                Scopes = scopes;
            }

            public bool Approved { get; set; }
            public string UserCode { get; }
            public string DeviceCode { get; }
            public string[] Scopes { get; }
        }

        public OAuth2Application(string id)
        {
            Id = id;
        }

        public string Id { get; }

        public string Secret { get; set; }

        public Uri[] RedirectUris { get; set; }

        public IList<AuthCodeGrant> AuthGrants { get; } = new List<AuthCodeGrant>();

        public IList<DeviceCodeGrant> DeviceGrants { get; } = new List<DeviceCodeGrant>();

        public IDictionary<string, string> AccessTokens { get; } = new Dictionary<string, string>();

        public IDictionary<string, string[]> RefreshTokens { get; } = new Dictionary<string, string[]>();

        public AuthCodeGrant CreateAuthorizationCodeGrant(TestOAuth2ServerTokenGenerator generator,
            string[] scopes, string redirectUri, string codeChallenge, OAuth2PkceChallengeMethod codeChallengeMethod)
        {
            string code = generator.CreateAuthorizationCode();

            var grant = new AuthCodeGrant(code, scopes, redirectUri, codeChallenge, codeChallengeMethod);
            AuthGrants.Add(grant);

            return grant;
        }

        public DeviceCodeGrant CreateDeviceCodeGrant(TestOAuth2ServerTokenGenerator generator, string[] scopes)
        {
            string deviceCode = generator.CreateDeviceCode();
            string userCode = generator.CreateUserCode();

            var grant = new DeviceCodeGrant(userCode, deviceCode, scopes);
            DeviceGrants.Add(grant);

            return grant;
        }

        public bool OwnsDeviceCodeGrant(string userCode)
        {
            return DeviceGrants.Any(x => x.UserCode == userCode);
        }

        public void ApproveDeviceCodeGrant(string userCode)
        {
            DeviceCodeGrant grant = DeviceGrants.FirstOrDefault(x => x.UserCode == userCode);

            if (grant is null)
            {
                throw new Exception($"Invalid user code '{userCode}'");
            }

            grant.Approved = true;
        }

        public bool IsDeviceCodeGrantApproved(string deviceCode)
        {
            DeviceCodeGrant grant = DeviceGrants.FirstOrDefault(x => x.DeviceCode == deviceCode);

            if (grant is null)
            {
                throw new Exception($"Invalid device code '{deviceCode}'");
            }

            return grant.Approved;
        }

        public TokenEndpointResponseJson CreateTokenByAuthorizationGrant(
            TestOAuth2ServerTokenGenerator generator, string authCode, string codeVerifier, string redirectUri)
        {
            var grant = AuthGrants.FirstOrDefault(x => x.Code == authCode);
            if (grant is null)
            {
                throw new Exception($"Invalid authorization code '{authCode}'");
            }

            // Validate the grant's code challenge was constructed from the given code verifier
            if (!string.IsNullOrWhiteSpace(grant.CodeChallenge))
            {
                if (string.IsNullOrWhiteSpace(codeVerifier))
                {
                    throw new Exception("Missing code verifier");
                }

                switch (grant.CodeChallengeMethod)
                {
                    case OAuth2PkceChallengeMethod.Sha256:
                        using (var sha256 = SHA256.Create())
                        {
                            string challenge = Base64UrlConvert.Encode(
                                sha256.ComputeHash(
                                    Encoding.ASCII.GetBytes(codeVerifier)
                                ),
                                false
                            );

                            if (challenge != grant.CodeChallenge)
                            {
                                throw new Exception($"Invalid code verifier '{codeVerifier}'");
                            }
                        }
                        break;

                    case OAuth2PkceChallengeMethod.Plain:
                        // The case matters!
                        if (!StringComparer.Ordinal.Equals(codeVerifier, grant.CodeChallenge))
                        {
                            throw new Exception($"Invalid code verifier '{codeVerifier}'");
                        }
                        break;
                }
            }

            // If an explicit redirect URI was used as part of the authorization request then
            // the redirect URI used for the token call must match exactly.
            if (!string.IsNullOrWhiteSpace(grant.RedirectUri) && !StringComparer.Ordinal.Equals(grant.RedirectUri, redirectUri))
            {
                throw new Exception("Redirect URI must match exactly the one used when requesting the authorization code.");
            }

            string accessToken = generator.CreateAccessToken();
            string refreshToken = generator.CreateRefreshToken();

            // Remove the auth code grant now we've generated an access token (do not allow auth code reuse)
            AuthGrants.Remove(grant);

            // Store the tokens
            AccessTokens[accessToken] = refreshToken;
            RefreshTokens[refreshToken] = grant.Scopes;

            return new TokenEndpointResponseJson
            {
                TokenType = Constants.Http.WwwAuthenticateBearerScheme,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Scope = string.Join(" ", grant.Scopes) // Keep the same scopes as before
            };
        }

        public TokenEndpointResponseJson CreateTokenByRefreshTokenGrant(TestOAuth2ServerTokenGenerator generator, string refreshToken)
        {
            if (!RefreshTokens.TryGetValue(refreshToken, out string[] scopes))
            {
                throw new Exception($"Invalid refresh token '{refreshToken}'");
            }

            string newAccessToken = generator.CreateAccessToken();
            string newRefreshToken = generator.CreateRefreshToken();

            // Store the tokens
            AccessTokens[newAccessToken] = newRefreshToken;
            RefreshTokens[newRefreshToken] = scopes;

            return new TokenEndpointResponseJson
            {
                TokenType = Constants.Http.WwwAuthenticateBearerScheme,
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                Scope = string.Join(" ", scopes) // Keep the same scopes as before
            };
        }

        public TokenEndpointResponseJson CreateTokenByDeviceCodeGrant(TestOAuth2ServerTokenGenerator generator, string deviceCode)
        {
            DeviceCodeGrant grant = DeviceGrants.FirstOrDefault(x => x.DeviceCode == deviceCode);

            if (grant is null)
            {
                throw new Exception($"Invalid user code '{deviceCode}'");
            }

            if (!grant.Approved)
            {
                throw new Exception($"Grant with device code '{deviceCode}' has not been approved'");
            }

            string accessToken = generator.CreateAccessToken();
            string refreshToken = generator.CreateRefreshToken();

            // Remove the device code grant now we've generated an access token (do not allow device code reuse)
            DeviceGrants.Remove(grant);

            // Store the tokens
            AccessTokens[accessToken] = refreshToken;
            RefreshTokens[refreshToken] = grant.Scopes;

            return new TokenEndpointResponseJson
            {
                TokenType = Constants.Http.WwwAuthenticateBearerScheme,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Scope = string.Join(" ", grant.Scopes) // Keep the same scopes as before
            };
        }

        private bool IsValidRedirect(Uri uri)
        {
            foreach (Uri redirectUri in RedirectUris)
            {
                if (redirectUri == uri)
                {
                    return true;
                }

                // For localhost we ignore the port number
                if (redirectUri.IsLoopback && uri.IsLoopback)
                {
                    var cmp = StringComparer.OrdinalIgnoreCase;

                    // Uri::Authority does not include port, whereas Uri::Host does
                    return cmp.Equals(redirectUri.Scheme, uri.Scheme) &&
                           cmp.Equals(redirectUri.Authority, uri.Authority) &&
                           cmp.Equals(redirectUri.GetComponents(UriComponents.Path, UriFormat.UriEscaped),
                               uri.GetComponents(UriComponents.Path, UriFormat.UriEscaped));
                }
            }

            return false;
        }

        internal Uri ValidateRedirect(string redirectStr)
        {
            // Use default redirect URI if one has not been specified for this grant
            if (redirectStr == null)
            {
                return RedirectUris.First();
            }

            if (!Uri.TryCreate(redirectStr, UriKind.Absolute, out Uri redirectUri))
            {
                throw new Exception($"Redirect '{redirectStr}' is not a valid URI");
            }

            if (!IsValidRedirect(redirectUri))
            {
                // If a redirect URI has been specified, it must match one of those that has been previously registered
                throw new Exception($"Redirect URI '{redirectUri}' does not match any registered values.");
            }

            return redirectUri;
        }
    }
}
