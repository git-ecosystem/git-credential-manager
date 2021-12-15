using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GitCredentialManager;
using GitCredentialManager.Authentication.OAuth;

namespace Atlassian.Bitbucket
{
    public class BitbucketHostProvider : IHostProvider
    {
        private readonly ICommandContext _context;
        private readonly IBitbucketAuthentication _bitbucketAuth;
        private readonly IBitbucketRestApi _bitbucketApi;

        public BitbucketHostProvider(ICommandContext context)
            : this(context, new BitbucketAuthentication(context), new BitbucketRestApi(context)) { }

        public BitbucketHostProvider(ICommandContext context, IBitbucketAuthentication bitbucketAuth, IBitbucketRestApi bitbucketApi)
        {
            EnsureArgument.NotNull(context, nameof(context));
            EnsureArgument.NotNull(bitbucketAuth, nameof(bitbucketAuth));
            EnsureArgument.NotNull(bitbucketApi, nameof(bitbucketApi));

            _context = context;
            _bitbucketAuth = bitbucketAuth;
            _bitbucketApi = bitbucketApi;
        }

        #region IHostProvider

        public string Id => "bitbucket";

        public string Name => "Bitbucket";

        public IEnumerable<string> SupportedAuthorityIds => BitbucketAuthentication.AuthorityIds;

        public bool IsSupported(InputArguments input)
        {
            if (input is null)
            {
                return false;
            }

            // Split port number and hostname from host input argument
            if (!input.TryGetHostAndPort(out string hostName, out _))
            {
                return false;
            }

            // We do not support unencrypted HTTP communications to Bitbucket,
            // but we report `true` here for HTTP so that we can show a helpful
            // error message for the user in `GetCredentialAsync`.
            return (StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "http") ||
                    StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "https")) &&
                   hostName.EndsWith(BitbucketConstants.BitbucketBaseUrlHost, StringComparison.OrdinalIgnoreCase);
        }

        public bool IsSupported(HttpResponseMessage response)
        {
            if (response is null)
            {
                return false;
            }

            // Identify Bitbucket on-prem instances from the HTTP response using the Atlassian specific header X-AREQUESTID
            var supported = response.Headers.Contains("X-AREQUESTID");

            _context.Trace.WriteLine($"Host is{(supported ? null : "n't")} supported as Bitbucket");

            return supported;
        }

        public async Task<ICredential> GetCredentialAsync(InputArguments input)
        {
            ValidateTargetUri(input);

            return await GetStoredCredentials(input) ?? await GetRefreshedCredentials(input);
        }

        private static void ValidateTargetUri(InputArguments input)
        {
            // Compute the remote URI
            Uri targetUri = input.GetRemoteUri();

            // We should not allow unencrypted communication and should inform the user
            if (StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "http")
                && IsBitbucketOrg(targetUri))
            {
                throw new Exception("Unencrypted HTTP is not supported for Bitbucket.org. Ensure the repository remote URL is using HTTPS.");
            }
        }

        private async Task<ICredential> GetStoredCredentials(InputArguments input)
        {
            if (_context.Settings.TryGetSetting(BitbucketConstants.EnvironmentVariables.AlwaysRefreshCredentials,
                Constants.GitConfiguration.Credential.SectionName, BitbucketConstants.GitConfiguration.Credential.AlwaysRefreshCredentials,
                out string alwaysRefreshCredentials) && alwaysRefreshCredentials.ToBooleanyOrDefault(false))
            {
                _context.Trace.WriteLine($"Ignore stored credentials");
                return null;
            }

            var targetUri = input.GetRemoteUri();
            string credentialService = GetServiceName(input);
            _context.Trace.WriteLine($"Look for existing credentials under {credentialService} ...");

            var credentials = _context.CredentialStore.Get(credentialService, input.UserName);

            if (credentials == null)
            {
                _context.Trace.WriteLine($"No stored credentials found");
                return null;
            }

            _context.Trace.WriteLineSecrets($"Found stored credentials: {credentials.Account}/{{0}}", new object[] { credentials.Password });

            // Check credentials are still valid
            if (!await ValidateCredentialsWork(input, credentials, GetSupportedAuthenticationModes(targetUri)))
            {
                return null;
            }

            return credentials;
        }

        private async Task<ICredential> GetRefreshedCredentials(InputArguments input)
        {
            _context.Trace.WriteLine($"Refresh credentials...");

            var targetUri = input.GetRemoteUri();

            // Check for presence of refresh_token entry in credential store
            var refreshTokenService = GetRefreshTokenServiceName(input);

            AuthenticationModes authModes = GetSupportedAuthenticationModes(targetUri);

            _context.Trace.WriteLine("Checking for refresh token...");
            ICredential refreshToken = SupportsOAuth(authModes) ? _context.CredentialStore.Get(refreshTokenService, input.UserName) : null;
            if (refreshToken is null)
            {
                _context.Trace.WriteLine($"No stored refresh token found");
                // There is no refresh token either because this is a non-2FA enabled account (where OAuth is not
                // required), or because we previously erased the RT.

                // Check for the presence of a credential in the store
                string credentialService = GetServiceName(input);

                if (SupportsBasicAuth(authModes))
                {
                    _context.Trace.WriteLine("Prompt for Basic Auth...");

                    var basicCredentials = await GetBasicCredentialsInteractive(input, authModes);

                    if (await ValidateCredentialsWork(input, basicCredentials, authModes))
                    {
                        return basicCredentials;
                    }

                    // Fall through to the start of the interactive OAuth authentication flow
                }

                if (SupportsOAuth(authModes))
                {
                    _context.Trace.WriteLine("Two-factor authentication is required - prompting for auth via OAuth...");
                    _context.Trace.WriteLine("Prompt for OAuth...");

                    // Show the 2FA/OAuth authentication required prompt
                    bool @continue = await _bitbucketAuth.ShowOAuthRequiredPromptAsync();
                    if (!@continue)
                    {
                        _context.Trace.WriteLine("User cancelled OAuth prompt");
                        throw new Exception("User cancelled OAuth authentication.");
                    }

                    // Fall through to the start of the interactive OAuth authentication flow
                }
            }
            else
            {
                _context.Trace.WriteLineSecrets($"Found stored refresh token: {{0}}", new object[] { refreshToken });

                // It's very likely that any access token expired between the last time we used/stored it.
                // To ensure the AT is as 'fresh' as it can be, always first try to use the refresh token
                // (which lives longer) to create a new AT (and possibly also a new RT).
                try
                {
                    return await GetOAuthCredentialsViaRefreshFlow(input, refreshToken);
                }
                catch (OAuth2Exception ex)
                {
                    _context.Trace.WriteLine("Failed to refresh existing OAuth credential using refresh token");
                    _context.Trace.WriteException(ex);

                    // We failed to refresh the AT using the RT; log the refresh failure and fall through to restart
                    // the OAuth authentication flow
                }
            }

            return await GetOAuthCredentialsViaInteractiveBrowserFlow(input);
        }

        private async Task<ICredential> GetBasicCredentialsInteractive(InputArguments input, AuthenticationModes authModes)
        {
            var targetUri = input.GetRemoteUri();

            // We don't have any credentials to use at all! Start with the assumption of no 2FA requirement
            // and capture username and password via an interactive prompt.
            var credential = await _bitbucketAuth.GetBasicCredentialsAsync(targetUri, input.UserName);
            if (credential is null)
            {
                _context.Trace.WriteLine("User cancelled Basic Auth prompt");
                throw new Exception("User cancelled Basic Auth prompt.");
            }

            // Either we have an existing credential (user/pass OR some form of token [PAT or AT]),
            // or we have a freshly captured user/pass. Regardless, we must check if these credentials
            // pass and two-factor requirement on the account.
            _context.Trace.WriteLine("Checking if two-factor requirements for stored credentials...");

            bool requires2Fa = await RequiresTwoFactorAuthenticationAsync(credential, authModes);
            if (!requires2Fa)
            {
                _context.Trace.WriteLine("Two-factor authentication not required");

                // Return the valid credential
                return credential;
            }

            return null;
        }

        private async Task<ICredential> GetOAuthCredentialsViaRefreshFlow(InputArguments input, ICredential refreshToken)
        {
            var refreshTokenService = GetRefreshTokenServiceName(input);
            _context.Trace.WriteLine("Refreshing OAuth credentials using refresh token...");

            OAuth2TokenResult refreshResult = await _bitbucketAuth.RefreshOAuthCredentialsAsync(refreshToken.Password);

            // Resolve the username
            _context.Trace.WriteLine("Resolving username for refreshed OAuth credential...");
            string refreshUserName = await ResolveOAuthUserNameAsync(refreshResult.AccessToken);
            _context.Trace.WriteLine($"Username for refreshed OAuth credential is '{refreshUserName}'");

            // Store the refreshed RT
            _context.Trace.WriteLine("Storing new refresh token...");
            _context.CredentialStore.AddOrUpdate(refreshTokenService, input.UserName, refreshResult.RefreshToken);

            // Return new access token
            return new GitCredential(refreshUserName, refreshResult.AccessToken);
        }

        private async Task<ICredential> GetOAuthCredentialsViaInteractiveBrowserFlow(InputArguments input)
        {
            var targetUri = input.GetRemoteUri();
            var refreshTokenService = GetRefreshTokenServiceName(input);

            // We failed to use the refresh token either because it didn't exist, or because the refresh token is no
            // longer valid. Either way we must now try authenticating using OAuth interactively.

            // Start OAuth authentication flow
            _context.Trace.WriteLine("Starting OAuth authentication flow...");
            OAuth2TokenResult oauthResult = await _bitbucketAuth.CreateOAuthCredentialsAsync(targetUri);

            // Resolve the username
            _context.Trace.WriteLine("Resolving username for OAuth credential...");
            string newUserName = await ResolveOAuthUserNameAsync(oauthResult.AccessToken);
            _context.Trace.WriteLine($"Username for OAuth credential is '{newUserName}'");

            // Store the new RT
            _context.Trace.WriteLine("Storing new refresh token...");
            _context.CredentialStore.AddOrUpdate(refreshTokenService, newUserName, oauthResult.RefreshToken);
            _context.Trace.WriteLine("Refresh token was successfully stored.");

            // Return the new AT as the credential
            return new GitCredential(newUserName, oauthResult.AccessToken);
        }

        private static bool SupportsOAuth(AuthenticationModes authModes)
        {
            return (authModes & AuthenticationModes.OAuth) != 0;
        }

        private static bool SupportsBasicAuth(AuthenticationModes authModes)
        {
            return (authModes & AuthenticationModes.Basic) != 0;
        }

        public AuthenticationModes GetSupportedAuthenticationModes(Uri targetUri)
        {
            if (!IsBitbucketOrg(targetUri))
            {
                // Bitbucket Server/DC should use Basic only
                return BitbucketConstants.ServerAuthenticationModes;
            }

            // Check for an explicit override for supported authentication modes
            if (_context.Settings.TryGetSetting(
                BitbucketConstants.EnvironmentVariables.AuthenticationModes,
                Constants.GitConfiguration.Credential.SectionName, BitbucketConstants.GitConfiguration.Credential.AuthenticationModes,
                out string authModesStr))
            {
                if (Enum.TryParse(authModesStr, true, out AuthenticationModes authModes) && authModes != AuthenticationModes.None)
                {
                    _context.Trace.WriteLine($"Supported authentication modes override present: {authModes}");
                    return authModes;
                }
                else
                {
                    _context.Trace.WriteLine($"Invalid value for supported authentication modes override setting: '{authModesStr}'");
                }
            }

            // Bitbucket.org should use Basic, OAuth or manual PAT based authentication only
            _context.Trace.WriteLine($"{targetUri} is bitbucket.org - authentication schemes: '{BitbucketConstants.DotOrgAuthenticationModes}'");
            return BitbucketConstants.DotOrgAuthenticationModes;

        }

        public Task StoreCredentialAsync(InputArguments input)
        {
            // It doesn't matter if this is an OAuth access token, or the literal username & password
            // because we store them the same way, against the same credential key in the store.
            // The OAuth refresh token is already stored on the 'get' request.
            string service = GetServiceName(input);

            _context.Trace.WriteLine("Storing credential...");
            _context.CredentialStore.AddOrUpdate(service, input.UserName, input.Password);
            _context.Trace.WriteLine("Credential was successfully stored.");

            return Task.CompletedTask;
        }

        public Task EraseCredentialAsync(InputArguments input)
        {
            // Erase the stored credential (which may be either the literal username & password, or
            // the OAuth access token). We don't need to erase the OAuth refresh token because on the
            // next 'get' request, if the RT is bad we will erase and reacquire a new one at that point.
            string service = GetServiceName(input);

            _context.Trace.WriteLine("Erasing credential...");
            if (_context.CredentialStore.Remove(service, input.UserName))
            {
                _context.Trace.WriteLine("Credential was successfully erased.");
            }
            else
            {
                _context.Trace.WriteLine("Credential was not erased.");
            }

            return Task.CompletedTask;
        }

        #endregion

        #region Private Methods

        private async Task<string> ResolveOAuthUserNameAsync(string accessToken)
        {
            RestApiResult<UserInfo> result = await _bitbucketApi.GetUserInformationAsync(null, accessToken, isBearerToken: true);
            if (result.Succeeded)
            {
                return result.Response.UserName;
            }

            throw new Exception($"Failed to resolve username. HTTP: {result.StatusCode}");
        }

        private async Task<string> ResolveBasicAuthUserNameAsync(string username, string password)
        {
            RestApiResult<UserInfo> result = await _bitbucketApi.GetUserInformationAsync(username, password, isBearerToken: false);
            if (result.Succeeded)
            {
                return result.Response.UserName;
            }

            throw new Exception($"Failed to resolve username. HTTP: {result.StatusCode}");
        }

        private async Task<bool> RequiresTwoFactorAuthenticationAsync(ICredential credentials, AuthenticationModes authModes)
        {
            _context.Trace.WriteLineSecrets($"Check if 2FA si required for credentials ({credentials.Account}/{{0}}) {authModes} ...", new object[] { credentials.Password });

            if (!SupportsOAuth(authModes))
            {
                return false;
            }

            RestApiResult<UserInfo> result = await _bitbucketApi.GetUserInformationAsync(
                credentials.Account, credentials.Password, isBearerToken: false);
            switch (result.StatusCode)
            {
                // 2FA may not be required
                case HttpStatusCode.OK:
                    return result.Response.IsTwoFactorAuthenticationEnabled;

                // 2FA is required
                case HttpStatusCode.Forbidden:
                    return true;

                // Incorrect credentials
                case HttpStatusCode.Unauthorized:
                    throw new Exception("Invalid credentials");

                default:
                    throw new Exception($"Unknown server response: {result.StatusCode}");
            }
        }

        private async Task<bool> ValidateCredentialsWork(InputArguments input, ICredential credentials, AuthenticationModes authModes)
        {
            if (credentials == null)
            {
                return false;
            }

            // TODO ideally we'd also check if the credentials have expired based on some local metadata (once/if we get such metadata storage), 
            // and return false if they have.
            // This would be more efficient than having to make REST API calls to check.

            var targetUri = input.GetRemoteUri();
            _context.Trace.WriteLineSecrets($"Validate credentials ({credentials.Account}/{{0}}) are fresh for {targetUri} ...", new object[] { credentials.Password });

            if (!IsBitbucketOrg(targetUri))
            {
                // TODO Validate DC/Server credentials before returning them to Git
                // Currently credentials for DC/Server are not checked by GCM.
                // Instead the validation relies on Git to try and fail with the credentials and then request GCM to erase them
                _context.Trace.WriteLine("For DC/Server skip validating existing credentials");
                return await Task.FromResult(true);
            }

            // Bitbucket supports both OAuth + Basic Auth unless there is explicit GCM configuration
            // So the credentials could be for either scheme therefore need to potentiall test both posibilities.
            if (SupportsOAuth(authModes))
            {
                try
                {
                    await ResolveOAuthUserNameAsync(credentials.Password);
                    _context.Trace.WriteLine("Validated existing credentials using OAuth");
                    return true;
                }
                catch (Exception)
                {
                    _context.Trace.WriteLine("Failed to validate existing credentials using OAuth");
                }
            }

            if (SupportsBasicAuth(authModes))
            {
                try
                {
                    await ResolveBasicAuthUserNameAsync(credentials.Account, credentials.Password);
                    _context.Trace.WriteLine("Validated existing credentials using BasicAuth");
                    return true;
                }
                catch (Exception)
                {
                    _context.Trace.WriteLine("Failed to validate existing credentials using Basic Auth");
                    return false;
                }
            }

            return true;
        }

        private static string GetServiceName(InputArguments input)
        {
            return input.GetRemoteUri(includeUser: false).AbsoluteUri.TrimEnd('/');
        }

        private static string GetRefreshTokenServiceName(InputArguments input)
        {
            Uri baseUri = input.GetRemoteUri(includeUser: false);

            // The refresh token key never includes the path component.
            // Instead we use the path component to specify this is the "refresh_token".
            Uri uri = new UriBuilder(baseUri) { Path = "/refresh_token" }.Uri;

            return uri.AbsoluteUri.TrimEnd('/');
        }

        public static bool IsBitbucketOrg(string targetUrl)
        {
            return Uri.TryCreate(targetUrl, UriKind.Absolute, out Uri uri) && IsBitbucketOrg(uri);
        }

        public static bool IsBitbucketOrg(Uri targetUri)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(targetUri.Host, BitbucketConstants.BitbucketBaseUrlHost);
        }

        #endregion

        public void Dispose()
        {
            _bitbucketApi.Dispose();
            _bitbucketAuth.Dispose();
        }
    }
}
