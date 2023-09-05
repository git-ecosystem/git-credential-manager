using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GitCredentialManager;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitHub
{
    public interface IGitHubRestApi : IDisposable
    {
        Task<AuthenticationResult> CreatePersonalAccessTokenAsync(
            Uri targetUri,
            string username,
            string password,
            string authenticationCode,
            IEnumerable<string> scopes);

        Task<GitHubUserInfo> GetUserInfoAsync(Uri targetUri, string accessToken);

        Task<GitHubMetaInfo> GetMetaInfoAsync(Uri targetUri);
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

        public async Task<AuthenticationResult> CreatePersonalAccessTokenAsync(Uri targetUri, string username, string password, string authenticationCode, IEnumerable<string> scopes)
        {
            EnsureArgument.AbsoluteUri(targetUri, nameof(targetUri));
            EnsureArgument.NotNull(scopes, nameof(scopes));

            Uri requestUri = GetApiRequestUri(targetUri, "authorizations");

            _context.Trace.WriteLine($"HTTP: POST {requestUri}");
            using (HttpContent content = GetTokenJsonContent(targetUri, scopes))
            using (var request = new HttpRequestMessage(HttpMethod.Post, requestUri))
            {
                // Set the request content as well as auth and 2FA headers
                request.Content = content;
                request.AddBasicAuthenticationHeader(username, password);
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

        public async Task<GitHubUserInfo> GetUserInfoAsync(Uri targetUri, string accessToken)
        {
            Uri requestUri = GetApiRequestUri(targetUri, "user");

            _context.Trace.WriteLine($"HTTP: GET {requestUri}");
            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                request.AddBearerAuthenticationHeader(accessToken);

                using (HttpResponseMessage response = await HttpClient.SendAsync(request))
                {
                    _context.Trace.WriteLine($"HTTP: Response {(int) response.StatusCode} [{response.StatusCode}]");

                    response.EnsureSuccessStatusCode();

                    string json = await response.Content.ReadAsStringAsync();

                    return JsonSerializer.Deserialize<GitHubUserInfo>(json);
                }
            }
        }

        public async Task<GitHubMetaInfo> GetMetaInfoAsync(Uri targetUri)
        {
            Uri requestUri = GetApiRequestUri(targetUri, "meta");

            _context.Trace.WriteLine($"HTTP: GET {requestUri}");
            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            using (HttpResponseMessage response = await HttpClient.SendAsync(request))
            {
                _context.Trace.WriteLine($"HTTP: Response {(int) response.StatusCode} [{response.StatusCode}]");

                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<GitHubMetaInfo>(json);
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

                return new AuthenticationResult(GitHubAuthenticationResultType.Success, password);
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
            string token = null;
            string responseText = await response.Content.ReadAsStringAsync();

            Match tokenMatch;
            if ((tokenMatch = Regex.Match(responseText, @"\s*""token""\s*:\s*""([^""]+)""\s*",
                    RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)).Success
                && tokenMatch.Groups.Count > 1)
            {
                token = tokenMatch.Groups[1].Value;
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

        internal /* for testing */ static Uri GetApiRequestUri(Uri targetUri, string apiUrl)
        {
            if (GitHubHostProvider.IsGitHubDotCom(targetUri))
            {
                return new Uri($"https://api.github.com/{apiUrl}");
            }
            else
            {
                // If we're here, it's GitHub Enterprise via a configured authority
                var baseUrl = targetUri.GetLeftPart(UriPartial.Authority);

                RegexOptions reOptions = RegexOptions.CultureInvariant | RegexOptions.IgnoreCase;

                // Check for 'raw.' in the hostname and remove it to get the correct GHE API URL
                baseUrl = Regex.Replace(baseUrl, @"^(https?://)raw\.", "$1", reOptions);

                // Likewise check for `gist.` in the hostname and remove it to get the correct GHE API URL
                baseUrl = Regex.Replace(baseUrl, @"^(https?://)gist\.", "$1", reOptions);

                return new Uri(baseUrl + $"/api/v3/{apiUrl}");
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

    public class GitHubUserInfo
    {
        [JsonPropertyName("login")]
        public string Login { get; set; }
    }

    public class GitHubMetaInfo
    {
        [JsonPropertyName("installed_version")]
        public string InstalledVersion { get; set; }

        [JsonPropertyName("verifiable_password_authentication")]
        public bool VerifiablePasswordAuthentication { get; set; }
    }

    public static class GitHubRestApiExtensions
    {
        public static Task<AuthenticationResult> CreatePersonalTokenAsync(
            this IGitHubRestApi api,
            Uri targetUri,
            ICredential credentials,
            string authenticationCode,
            IEnumerable<string> scopes)
        {
            return api.CreatePersonalAccessTokenAsync(
                targetUri,
                credentials?.Account,
                credentials?.Password,
                authenticationCode,
                scopes);
        }
    }
}
