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
            // We should not allow unencrypted communication and should inform the user
            if (StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "http")
                && input.TryGetHostAndPort(out string host, out _) && IsBitbucketOrg(host))
            {
                throw new Exception("Unencrypted HTTP is not supported for Bitbucket.org. Ensure the repository remote URL is using HTTPS.");
            }

            Uri remoteUri = input.GetRemoteUri();

            AuthenticationModes authModes = GetSupportedAuthenticationModes(remoteUri);

            return await GetStoredCredentials(remoteUri, input.UserName, authModes) ??
                   await GetRefreshedCredentials(remoteUri, input.UserName, authModes);
        }

        private async Task<ICredential> GetStoredCredentials(Uri remoteUri, string userName, AuthenticationModes authModes)
        {
            if (_context.Settings.TryGetSetting(BitbucketConstants.EnvironmentVariables.AlwaysRefreshCredentials,
                Constants.GitConfiguration.Credential.SectionName, BitbucketConstants.GitConfiguration.Credential.AlwaysRefreshCredentials,
                out string alwaysRefreshCredentials) && alwaysRefreshCredentials.ToBooleanyOrDefault(false))
            {
                _context.Trace.WriteLine("Ignore stored credentials");
                return null;
            }

            string credentialService = GetServiceName(remoteUri);
            _context.Trace.WriteLine($"Look for existing credentials under {credentialService} ...");

            ICredential credentials = _context.CredentialStore.Get(credentialService, userName);

            if (credentials == null)
            {
                _context.Trace.WriteLine("No stored credentials found");
                return null;
            }

            _context.Trace.WriteLineSecrets($"Found stored credentials: {credentials.Account}/{{0}}", new object[] { credentials.Password });

            // Check credentials are still valid
            if (!await ValidateCredentialsWork(remoteUri, credentials, authModes))
            {
                return null;
            }

            return credentials;
        }

        private async Task<ICredential> GetRefreshedCredentials(Uri remoteUri, string userName, AuthenticationModes authModes)
        {
            _context.Trace.WriteLine("Refresh credentials...");

            // Check for presence of refresh_token entry in credential store
            var refreshTokenService = GetRefreshTokenServiceName(remoteUri);

            _context.Trace.WriteLine("Checking for refresh token...");
            ICredential refreshToken = SupportsOAuth(authModes)
                ? _context.CredentialStore.Get(refreshTokenService, userName)
                : null;

            if (refreshToken is null)
            {
                _context.Trace.WriteLine("No stored refresh token found");
                // There is no refresh token either because this is a non-2FA enabled account (where OAuth is not
                // required), or because we previously erased the RT.

                _context.Trace.WriteLine("Prompt for credentials...");

                var result = await _bitbucketAuth.GetCredentialsAsync(remoteUri, userName, authModes);
                if (result is null || result.AuthenticationMode == AuthenticationModes.None)
                {
                    _context.Trace.WriteLine("User cancelled credential prompt");
                    throw new Exception("User cancelled credential prompt.");
                }

                switch (result.AuthenticationMode)
                {
                    case AuthenticationModes.Basic:
                        // Return the valid credential
                        return result.Credential;

                    case AuthenticationModes.OAuth:
                        // If the user wants to use OAuth fall through to interactive auth
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(
                            $"Unexpected {nameof(AuthenticationModes)} returned from prompt");
                }

                // Fall through to the start of the interactive OAuth authentication flow
            }
            else
            {
                _context.Trace.WriteLineSecrets("Found stored refresh token: {0}", new object[] { refreshToken });

                try
                {
                    return await GetOAuthCredentialsViaRefreshFlow(remoteUri, refreshToken);
                }
                catch (OAuth2Exception ex)
                {
                    _context.Trace.WriteLine("Failed to refresh existing OAuth credential using refresh token");
                    _context.Trace.WriteException(ex);

                    // We failed to refresh the AT using the RT; log the refresh failure and fall through to restart
                    // the OAuth authentication flow
                }
            }

            return await GetOAuthCredentialsViaInteractiveBrowserFlow(remoteUri);
        }

        private async Task<ICredential> GetOAuthCredentialsViaRefreshFlow(Uri remoteUri, ICredential refreshToken)
        {
            var refreshTokenService = GetRefreshTokenServiceName(remoteUri);
            _context.Trace.WriteLine("Refreshing OAuth credentials using refresh token...");

            OAuth2TokenResult refreshResult = await _bitbucketAuth.RefreshOAuthCredentialsAsync(refreshToken.Password);

            // Resolve the username
            _context.Trace.WriteLine("Resolving username for refreshed OAuth credential...");
            string refreshUserName = await ResolveOAuthUserNameAsync(refreshResult.AccessToken);
            _context.Trace.WriteLine($"Username for refreshed OAuth credential is '{refreshUserName}'");

            // Store the refreshed RT
            _context.Trace.WriteLine("Storing new refresh token...");
            _context.CredentialStore.AddOrUpdate(refreshTokenService, remoteUri.GetUserName(), refreshResult.RefreshToken);

            // Return new access token
            return new GitCredential(refreshUserName, refreshResult.AccessToken);
        }

        private async Task<ICredential> GetOAuthCredentialsViaInteractiveBrowserFlow(Uri remoteUri)
        {
            var refreshTokenService = GetRefreshTokenServiceName(remoteUri);

            // We failed to use the refresh token either because it didn't exist, or because the refresh token is no
            // longer valid. Either way we must now try authenticating using OAuth interactively.

            // Start OAuth authentication flow
            _context.Trace.WriteLine("Starting OAuth authentication flow...");
            OAuth2TokenResult oauthResult = await _bitbucketAuth.CreateOAuthCredentialsAsync(remoteUri);

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
            Uri remoteUri = input.GetRemoteUri();
            string service = GetServiceName(remoteUri);

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
            Uri remoteUri = input.GetRemoteUri();
            string service = GetServiceName(remoteUri);

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

        private async Task<bool> ValidateCredentialsWork(Uri remoteUri, ICredential credentials, AuthenticationModes authModes)
        {
            if (credentials is null)
            {
                return false;
            }

            // TODO: ideally we'd also check if the credentials have expired based on some local metadata
            // (once/if we get such metadata storage), and return false if they have.
            // This would be more efficient than having to make REST API calls to check.

            _context.Trace.WriteLineSecrets($"Validate credentials ({credentials.Account}/{{0}}) are fresh for {remoteUri} ...", new object[] { credentials.Password });

            if (!IsBitbucketOrg(remoteUri))
            {
                // TODO: Validate DC/Server credentials before returning them to Git
                // Currently credentials for DC/Server are not checked by GCM.
                // Instead the validation relies on Git to try and fail with the credentials and then request GCM to erase them
                _context.Trace.WriteLine("For DC/Server skip validating existing credentials");
                return await Task.FromResult(true);
            }

            // Bitbucket supports both OAuth + Basic Auth unless there is explicit GCM configuration.
            // The credentials could be for either scheme therefore need to potentially test both possibilities.
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

        private static string GetServiceName(Uri remoteUri)
        {
            return remoteUri.WithoutUserInfo().AbsoluteUri.TrimEnd('/');
        }

        internal /* for testing */ static string GetRefreshTokenServiceName(Uri remoteUri)
        {
            Uri baseUri = remoteUri.WithoutUserInfo();

            // The refresh token key never includes the path component.
            // Instead we use the path component to specify this is the "refresh_token".
            Uri uri = new UriBuilder(baseUri) { Path = "/refresh_token" }.Uri;

            return uri.AbsoluteUri.TrimEnd('/');
        }

        public static bool IsBitbucketOrg(Uri targetUri)
        {
            return IsBitbucketOrg(targetUri.Host);
        }

        public static bool IsBitbucketOrg(string host)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(host, BitbucketConstants.BitbucketBaseUrlHost);
        }

        #endregion

        public void Dispose()
        {
            _bitbucketApi.Dispose();
            _bitbucketAuth.Dispose();
        }
    }
}
