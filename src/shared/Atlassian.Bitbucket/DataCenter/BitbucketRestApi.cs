using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GitCredentialManager;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atlassian.Bitbucket.DataCenter
{
    public class BitbucketRestApi : IBitbucketRestApi
    {
        private readonly ICommandContext _context;

        private HttpClient _httpClient;

        public BitbucketRestApi(ICommandContext context)
        {
            EnsureArgument.NotNull(context, nameof(context));

            _context = context;

        }

        public async Task<RestApiResult<IUserInfo>> GetUserInformationAsync(string userName, string password, bool isBearerToken)
        {
            if (_context.Settings.TryGetSetting(
                BitbucketConstants.EnvironmentVariables.ValidateStoredCredentials,
                Constants.GitConfiguration.Credential.SectionName, BitbucketConstants.GitConfiguration.Credential.ValidateStoredCredentials,
                out string validateStoredCredentials) && !validateStoredCredentials.ToBooleanyOrDefault(true))
            {
                _context.Trace.WriteLine($"Skipping retreival of user information due to {BitbucketConstants.GitConfiguration.Credential.ValidateStoredCredentials} = {validateStoredCredentials}");
                return new RestApiResult<IUserInfo>(HttpStatusCode.OK, new UserInfo() { UserName = DataCenterConstants.OAuthUserName });;
            }

            // Bitbucket Server/DC doesn't actually provide a REST API we can use to trade an access_token for the owning username,
            // therefore this is always going to return a placeholder username, however this call does provide a way to validate the
            // credentials we do have
            var requestUri = new Uri(ApiUri, "api/1.0/users");
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                if (isBearerToken)
                {
                    request.AddBearerAuthenticationHeader(password);
                }
                else
                {
                    request.AddBasicAuthenticationHeader(userName, password);
                }

                _context.Trace.WriteLine($"HTTP: GET {requestUri}");
                using (HttpResponseMessage response = await HttpClient.SendAsync(request))
                {
                    _context.Trace.WriteLine($"HTTP: Response {(int) response.StatusCode} [{response.StatusCode}]");

                    string json = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        // No REST API in BBS that can be used to return just my user account based on my login AFAIK.
                        // but we can prove the credentials work.
                        return new RestApiResult<IUserInfo>(HttpStatusCode.OK, new UserInfo() { UserName = DataCenterConstants.OAuthUserName });
                    }

                    return new RestApiResult<IUserInfo>(response.StatusCode);
                }
            }

        }

        public async Task<bool> IsOAuthInstalledAsync()
        {
            var requestUri = new Uri(ApiUri.AbsoluteUri + "oauth2/1.0/client");
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                _context.Trace.WriteLine($"HTTP: GET {requestUri}");
                using (HttpResponseMessage response = await HttpClient.SendAsync(request))
                {
                    _context.Trace.WriteLine($"HTTP: Response {(int)response.StatusCode} [{response.StatusCode}]");

                    if (HttpStatusCode.Unauthorized == response.StatusCode)
                    {
                        // accessed anonymously so no access but it does exist.
                        return true;
                    }

                    return false;
                }
            }
        }

        public async Task<List<AuthenticationMethod>> GetAuthenticationMethodsAsync()
        {
            var authenticationMethods = new List<AuthenticationMethod>();

            var requestUri = new Uri(ApiUri.AbsoluteUri + "authconfig/1.0/login-options");
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                _context.Trace.WriteLine($"HTTP: GET {requestUri}");
                using (HttpResponseMessage response = await HttpClient.SendAsync(request))
                {
                    _context.Trace.WriteLine($"HTTP: Response {(int)response.StatusCode} [{response.StatusCode}]");

                    string json = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var loginOptions = JsonSerializer.Deserialize<LoginOptions>(json,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true,
                                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                            });

                        if (loginOptions.Results.Any(r => "LOGIN_FORM".Equals(r.Type)))
                        {
                            authenticationMethods.Add(AuthenticationMethod.BasicAuth);
                        }

                        if (loginOptions.Results.Any(r => "IDP".Equals(r.Type)))
                        {
                            authenticationMethods.Add(AuthenticationMethod.Sso);
                        }
                    }
                }
            }

            return authenticationMethods;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        private HttpClient HttpClient => _httpClient ??= _context.HttpClientFactory.CreateClient();

        private Uri ApiUri
        {
            get
            {
                var remoteUri = _context.Settings?.RemoteUri;
                if (remoteUri == null)
                {
                    throw new ArgumentException("RemoteUri must be defined to generate Bitbucket DC OAuth2 endpoint Urls");
                }

                return new Uri(BitbucketHelper.GetBaseUri(remoteUri) + "/rest/");
            }
        }
    }
}