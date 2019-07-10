// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;

namespace GitHub
{
    public interface IGitHubRestApi : IDisposable
    {
        Task<AuthenticationResult> AcquireTokenAsync(
            Uri targetUri,
            string username,
            string password,
            string authenticationCode,
            IEnumerable<string> scopes);
    }

    public class GitHubRestApi : IGitHubRestApi
    {
        /// <summary>
        /// The maximum wait time for a network request before timing out
        /// </summary>
        private const int RequestTimeout = 15 * 1000; // 15 second limit

        private readonly ICommandContext _context;

        public GitHubRestApi(ICommandContext context)
        {
            EnsureArgument.NotNull(context, nameof(context));

            _context = context;
        }

        #region IGitHubApi

        public async Task<AuthenticationResult> AcquireTokenAsync(Uri targetUri, string username, string password, string authenticationCode, IEnumerable<string> scopes)
        {
            EnsureArgument.AbsoluteUri(targetUri, nameof(targetUri));
            EnsureArgument.NotNull(scopes, nameof(scopes));

            string base64Cred = new GitCredential(username, password).ToBase64String();

            Uri requestUri = GetAuthenticationRequestUri(targetUri);

            _context.Trace.WriteLine($"HTTP: POST {requestUri}");
            using (HttpContent content = GetTokenJsonContent(targetUri, scopes))
            using (var request = new HttpRequestMessage(HttpMethod.Post, requestUri))
            {
                // Set the request content as well as auth and 2FA headers
                request.Content = content;
                request.Headers.Authorization = new AuthenticationHeaderValue(Constants.Http.WwwAuthenticateBasicScheme, base64Cred);
                if (!string.IsNullOrWhiteSpace(authenticationCode))
                {
                    request.Headers.Add(GitHubConstants.GitHubOptHeader, authenticationCode);
                }

                // Send the request!
                using (HttpResponseMessage response = await HttpClient.SendAsync(request))
                {
                    _context.Trace.WriteLine($"HTTP: Response {(int) response.StatusCode} [{response.StatusCode}]");

                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.OK:
                        case HttpStatusCode.Created:
                            return await ParseSuccessResponseAsync(targetUri, response);

                        case HttpStatusCode.Unauthorized:
                            return ParseUnauthorizedResponse(targetUri, authenticationCode, response);

                        case HttpStatusCode.Forbidden:
                            return await ParseForbiddenResponseAsync(targetUri, password, response);

                        default:
                            _context.Trace.WriteLine($"Authentication failed for '{targetUri}'.");
                            return new AuthenticationResult(GitHubAuthenticationResultType.Failure);
                    }
                }
            }
        }

        #endregion

        #region Private Methods

        private async Task<AuthenticationResult> ParseForbiddenResponseAsync(Uri targetUri, string password, HttpResponseMessage response)
        {
            // This API only supports Basic authentication. If a valid OAuth token is supplied
            // as the password, then a Forbidden response is returned instead of an Unauthorized.
            // In that case, the supplied password is an OAuth token and is valid and we don't need
            // to create a new personal access token.
            var contentBody = await response.Content.ReadAsStringAsync();
            if (contentBody.Contains("This API can only be accessed with username and password Basic Auth"))
            {
                _context.Trace.WriteLine($"Authentication success: user supplied personal access token for '{targetUri}'.");

                return new AuthenticationResult(GitHubAuthenticationResultType.Success,
                    new GitCredential(Constants.PersonalAccessTokenUserName, password));
            }

            _context.Trace.WriteLine($"Authentication failed for '{targetUri}'.");
            return new AuthenticationResult(GitHubAuthenticationResultType.Failure);
        }

        private AuthenticationResult ParseUnauthorizedResponse(Uri targetUri, string authenticationCode, HttpResponseMessage response)
        {
            if (string.IsNullOrWhiteSpace(authenticationCode)
                && response.Headers.Any(x => StringComparer.OrdinalIgnoreCase.Equals(GitHubConstants.GitHubOptHeader, x.Key)))
            {
                var mfakvp = response.Headers.First(x =>
                    StringComparer.OrdinalIgnoreCase.Equals(GitHubConstants.GitHubOptHeader, x.Key) &&
                    x.Value != null && x.Value.Any());

                if (mfakvp.Value.First().Contains("app"))
                {
                    _context.Trace.WriteLine($"Two-factor app authentication code required for '{targetUri}'.");
                    return new AuthenticationResult(GitHubAuthenticationResultType.TwoFactorApp);
                }
                else
                {
                    _context.Trace.WriteLine($"Two-factor SMS authentication code required for '{targetUri}'.");
                    return new AuthenticationResult(GitHubAuthenticationResultType.TwoFactorSms);
                }
            }
            else
            {
                _context.Trace.WriteLine($"Authentication failed for '{targetUri}'.");
                return new AuthenticationResult(GitHubAuthenticationResultType.Failure);
            }
        }

        private async Task<AuthenticationResult> ParseSuccessResponseAsync(Uri targetUri, HttpResponseMessage response)
        {
            GitCredential token = null;
            string responseText = await response.Content.ReadAsStringAsync();

            Match tokenMatch;
            if ((tokenMatch = Regex.Match(responseText, @"\s*""token""\s*:\s*""([^""]+)""\s*",
                    RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)).Success
                && tokenMatch.Groups.Count > 1)
            {
                string tokenText = tokenMatch.Groups[1].Value;
                token = new GitCredential(Constants.PersonalAccessTokenUserName, tokenText);
            }

            if (token == null)
            {
                _context.Trace.WriteLine($"Authentication for '{targetUri}' failed.");
                return new AuthenticationResult(GitHubAuthenticationResultType.Failure);
            }
            else
            {
                _context.Trace.WriteLine($"Authentication success: new personal access token for '{targetUri}' created.");
                return new AuthenticationResult(GitHubAuthenticationResultType.Success, token);
            }
        }

        private Uri GetAuthenticationRequestUri(Uri targetUri)
        {
            if (targetUri.DnsSafeHost.Equals(GitHubConstants.GitHubBaseUrlHost, StringComparison.OrdinalIgnoreCase))
            {
                return new Uri("https://api.github.com/authorizations");
            }
            else
            {
                // If we're here, it's GitHub Enterprise via a configured authority
                var baseUrl = targetUri.GetLeftPart(UriPartial.Authority);
                return new Uri(baseUrl + "/api/v3/authorizations");
            }
        }

        private HttpContent GetTokenJsonContent(Uri targetUri, IEnumerable<string> scopes)
        {
            const string HttpJsonContentType = "application/x-www-form-urlencoded";
            const string JsonContentFormat = @"{{ ""scopes"": {0}, ""note"": ""git: {1} on {2} at {3:dd-MMM-yyyy HH:mm}"" }}";

            var quotedScopes = scopes.Select(x => $"\"{x}\"");
            string scopesJson = $"[{string.Join(", ", quotedScopes)}]";

            string jsonContent = string.Format(JsonContentFormat, scopesJson, targetUri, Environment.MachineName, DateTime.Now);

            return new StringContent(jsonContent, Encoding.UTF8, HttpJsonContentType);
        }

        private HttpClient _httpClient;
        private HttpClient HttpClient
        {
            get
            {
                if (_httpClient is null)
                {
                    _httpClient = _context.HttpClientFactory.CreateClient();

                    // Set the common headers and timeout
                    _httpClient.Timeout = TimeSpan.FromMilliseconds(RequestTimeout);
                    _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(GitHubConstants.GitHubApiAcceptsHeaderValue));
                }

                return _httpClient;
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        #endregion
    }

    public static class GitHubRestApiExtensions
    {
        public static Task<AuthenticationResult> AcquireTokenAsync(
            this IGitHubRestApi api,
            Uri targetUri,
            ICredential credentials,
            string authenticationCode,
            IEnumerable<string> scopes)
        {
            return api.AcquireTokenAsync(
                targetUri,
                credentials?.UserName,
                credentials?.Password,
                authenticationCode,
                scopes);
        }
    }
}
