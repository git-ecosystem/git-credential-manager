using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Atlassian.Bitbucket.Cloud;
using GitCredentialManager;
using GitCredentialManager.Authentication.OAuth;

namespace Atlassian.Bitbucket
{
    public class BitbucketHostProvider : IHostProvider
    {
        private readonly ICommandContext _context;
        private readonly IBitbucketAuthentication _bitbucketAuth;
        private readonly IRegistry<IBitbucketRestApi> _restApiRegistry;

        public BitbucketHostProvider(ICommandContext context)
            : this(context, new BitbucketAuthentication(context), new BitbucketRestApiRegistry(context)) { }

        public BitbucketHostProvider(ICommandContext context, IBitbucketAuthentication bitbucketAuth, IRegistry<IBitbucketRestApi> restApiRegistry)
        {
            EnsureArgument.NotNull(context, nameof(context));
            EnsureArgument.NotNull(bitbucketAuth, nameof(bitbucketAuth));
            EnsureArgument.NotNull(restApiRegistry, nameof(restApiRegistry));

            _context = context;
            _bitbucketAuth = bitbucketAuth;
            _restApiRegistry = restApiRegistry;
        }

        #region IHostProvider

        public string Id => BitbucketConstants.Id;

        public string Name => BitbucketConstants.Name;

        public IEnumerable<string> SupportedAuthorityIds => BitbucketAuthentication.AuthorityIds;

        public bool IsSupported(InputArguments input)
        {
            if (input is null)
            {
                return false;
            }

            if (input.WwwAuth.Any(x => x.Contains("realm=\"Atlassian Bitbucket\"", StringComparison.InvariantCultureIgnoreCase)))
            {
                return true;
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
                    hostName.EndsWith(CloudConstants.BitbucketBaseUrlHost, StringComparison.OrdinalIgnoreCase);
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
                && BitbucketHelper.IsBitbucketOrg(input))
            {
                throw new Trace2Exception(_context.Trace2,
                    "Unencrypted HTTP is not supported for Bitbucket.org. Ensure the repository remote URL is using HTTPS.");
            }

            var authModes = await GetSupportedAuthenticationModesAsync(input);

            return await GetStoredCredentials(input, authModes) ??
                   await GetRefreshedCredentials(input, authModes);
        }

        private async Task<ICredential> GetStoredCredentials(InputArguments input, AuthenticationModes authModes)
        {
            if (_context.Settings.TryGetSetting(BitbucketConstants.EnvironmentVariables.AlwaysRefreshCredentials,
                Constants.GitConfiguration.Credential.SectionName, BitbucketConstants.GitConfiguration.Credential.AlwaysRefreshCredentials,
                out string alwaysRefreshCredentials) && alwaysRefreshCredentials.ToBooleanyOrDefault(false))
            {
                _context.Trace.WriteLine("Ignore stored credentials");
                return null;
            }

            Uri remoteUri = input.GetRemoteUri();
            string credentialService = GetServiceName(remoteUri);
            _context.Trace.WriteLine($"Look for existing credentials under {credentialService} ...");

            ICredential credentials = _context.CredentialStore.Get(credentialService, input.UserName);

            if (credentials == null)
            {
                _context.Trace.WriteLine("No stored credentials found");
                return null;
            }

            _context.Trace.WriteLineSecrets($"Found stored credentials: {credentials.Account}/{{0}}", new object[] { credentials.Password });

            // Check credentials are still valid
            if (!await ValidateCredentialsWork(input, credentials, authModes))
            {
                return null;
            }

            return credentials;
        }

        private async Task<ICredential> GetRefreshedCredentials(InputArguments input, AuthenticationModes authModes)
        {
            _context.Trace.WriteLine("Refresh credentials...");

            // Check for presence of refresh_token entry in credential store
            Uri remoteUri = input.GetRemoteUri();
            var refreshTokenService = GetRefreshTokenServiceName(remoteUri);

            _context.Trace.WriteLine("Checking for refresh token...");
            ICredential refreshToken = SupportsOAuth(authModes)
                ? _context.CredentialStore.Get(refreshTokenService, input.UserName)
                : null;

            if (refreshToken is null)
            {
                _context.Trace.WriteLine("No stored refresh token found");
                // There is no refresh token either because this is a non-2FA enabled account (where OAuth is not
                // required), or because we previously erased the RT.

                _context.Trace.WriteLine("Prompt for credentials...");

                var result = await _bitbucketAuth.GetCredentialsAsync(remoteUri, input.UserName, authModes);
                if (result is null || result.AuthenticationMode == AuthenticationModes.None)
                {
                    var message = "User cancelled credential prompt";
                    _context.Trace.WriteLine(message);
                    throw new Trace2Exception(_context.Trace2, message);
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
                    return await GetOAuthCredentialsViaRefreshFlow(input, refreshToken);
                }
                catch (OAuth2Exception ex)
                {
                    var message = "Failed to refresh existing OAuth credential using refresh token";
                    _context.Trace.WriteLine(message);
                    _context.Trace.WriteException(ex);
                    _context.Trace2.WriteError(message);

                    // We failed to refresh the AT using the RT; log the refresh failure and fall through to restart
                    // the OAuth authentication flow
                }
            }

            return await GetOAuthCredentialsViaInteractiveBrowserFlow(input);
        }

        private async Task<ICredential> GetOAuthCredentialsViaRefreshFlow(InputArguments input, ICredential refreshToken)
        {
            Uri remoteUri = input.GetRemoteUri();

            var refreshTokenService = GetRefreshTokenServiceName(remoteUri);
            _context.Trace.WriteLine("Refreshing OAuth credentials using refresh token...");

            OAuth2TokenResult refreshResult = await _bitbucketAuth.RefreshOAuthCredentialsAsync(input, refreshToken.Password);

            // Resolve the username
            _context.Trace.WriteLine("Resolving username for refreshed OAuth credential...");
            string refreshUserName = await ResolveOAuthUserNameAsync(input, refreshResult.AccessToken);
            _context.Trace.WriteLine($"Username for refreshed OAuth credential is '{refreshUserName}'");

            // Store the refreshed RT
            _context.Trace.WriteLine("Storing new refresh token...");
            _context.CredentialStore.AddOrUpdate(refreshTokenService, remoteUri.GetUserName(), refreshResult.RefreshToken);

            // Return new access token
            return new GitCredential(refreshUserName, refreshResult.AccessToken);
        }

        private async Task<ICredential> GetOAuthCredentialsViaInteractiveBrowserFlow(InputArguments input)
        {
            Uri remoteUri = input.GetRemoteUri();

            var refreshTokenService = GetRefreshTokenServiceName(remoteUri);

            // We failed to use the refresh token either because it didn't exist, or because the refresh token is no
            // longer valid. Either way we must now try authenticating using OAuth interactively.

            // Start OAuth authentication flow
            _context.Trace.WriteLine("Starting OAuth authentication flow...");
            OAuth2TokenResult oauthResult = await _bitbucketAuth.CreateOAuthCredentialsAsync(input);

            // Resolve the username
            _context.Trace.WriteLine("Resolving username for OAuth credential...");
            string newUserName = await ResolveOAuthUserNameAsync(input, oauthResult.AccessToken);
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

        public async Task<AuthenticationModes> GetSupportedAuthenticationModesAsync(InputArguments input)
        {
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

            // It isn't possible to detect what Bitbucket.org is expecting so return the predefined answers.
            if (BitbucketHelper.IsBitbucketOrg(input))
            {
                // Bitbucket should use Basic, OAuth or manual PAT based authentication only
                _context.Trace.WriteLine($"{input.GetRemoteUri()} is bitbucket.org - authentication schemes: '{CloudConstants.DotOrgAuthenticationModes}'");
                return CloudConstants.DotOrgAuthenticationModes;
            }

            // For Bitbucket DC/Server the supported modes can be detected
            _context.Trace.WriteLine($"{input.GetRemoteUri()} is Bitbucket DC - checking for supported authentication schemes...");

            try
            {
                var authenticationMethods = await _restApiRegistry.Get(input).GetAuthenticationMethodsAsync();

                var modes = AuthenticationModes.None;

                if (authenticationMethods.Contains(AuthenticationMethod.BasicAuth))
                {
                    modes |= AuthenticationModes.Basic;
                }

                var isOauthInstalled = await _restApiRegistry.Get(input).IsOAuthInstalledAsync();
                if (isOauthInstalled)
                {
                    modes |= AuthenticationModes.OAuth;
                }

                _context.Trace.WriteLine($"Bitbucket DC/Server instance supports authentication schemes: {modes}");
                return modes;
            }
            catch (Exception ex)
            {
                var format = "Failed to query '{0}' for supported authentication schemes.";
                var message = string.Format(format, input.GetRemoteUri());

                _context.Trace.WriteLine(message);
                _context.Trace.WriteException(ex);
                _context.Trace2.WriteError(message, format);

                _context.Terminal.WriteLine($"warning: {message}");

                // Fall-back to offering all modes so the user is never blocked from authenticating by at least one mode
                return AuthenticationModes.All;
            }
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

        private async Task<string> ResolveOAuthUserNameAsync(InputArguments input, string accessToken)
        {
            RestApiResult<IUserInfo> result = await _restApiRegistry.Get(input).GetUserInformationAsync(null, accessToken, isBearerToken: true);
            if (result.Succeeded)
            {
                return result.Response.UserName;
            }

            throw new Trace2Exception(_context.Trace2,
                $"Failed to resolve username. HTTP: {result.StatusCode}");
        }

        private async Task<string> ResolveBasicAuthUserNameAsync(InputArguments input, string username, string password)
        {
            RestApiResult<IUserInfo> result = await _restApiRegistry.Get(input).GetUserInformationAsync(username, password, isBearerToken: false);
            if (result.Succeeded)
            {
                return result.Response.UserName;
            }

            throw new Trace2Exception(_context.Trace2,
                $"Failed to resolve username. HTTP: {result.StatusCode}");
        }

        private async Task<bool> ValidateCredentialsWork(InputArguments input, ICredential credentials, AuthenticationModes authModes)
        {
            if (_context.Settings.TryGetSetting(
                BitbucketConstants.EnvironmentVariables.ValidateStoredCredentials,
                Constants.GitConfiguration.Credential.SectionName, BitbucketConstants.GitConfiguration.Credential.ValidateStoredCredentials,
                out string validateStoredCredentials) && !validateStoredCredentials.ToBooleanyOrDefault(true))
            {
                _context.Trace.WriteLine($"Skipping validation of stored credentials due to {BitbucketConstants.GitConfiguration.Credential.ValidateStoredCredentials} = {validateStoredCredentials}");
                return true;
            }

            if (credentials is null)
            {
                return false;
            }

            // TODO: ideally we'd also check if the credentials have expired based on some local metadata
            // (once/if we get such metadata storage), and return false if they have.
            // This would be more efficient than having to make REST API calls to check.
            Uri remoteUri = input.GetRemoteUri();
            _context.Trace.WriteLineSecrets($"Validate credentials ({credentials.Account}/{{0}}) are fresh for {remoteUri} ...", new object[] { credentials.Password });

            // Bitbucket supports both OAuth + Basic Auth unless there is explicit GCM configuration.
            // The credentials could be for either scheme therefore need to potentially test both possibilities.
            if (SupportsOAuth(authModes))
            {
                try
                {
                    await ResolveOAuthUserNameAsync(input, credentials.Password);
                    _context.Trace.WriteLine("Validated existing credentials using OAuth");
                    return true;
                }
                catch (Exception ex)
                {
                    var message = "Failed to validate existing credentials using OAuth";
                    _context.Trace.WriteLine(message);
                    _context.Trace.WriteException(ex);
                    _context.Trace2.WriteError(message);
                }
            }

            if (SupportsBasicAuth(authModes))
            {
                try
                {
                    await ResolveBasicAuthUserNameAsync(input, credentials.Account, credentials.Password);
                    _context.Trace.WriteLine("Validated existing credentials using BasicAuth");
                    return true;
                }
                catch (Exception ex)
                {
                    var message = "Failed to validate existing credentials using Basic Auth";
                    _context.Trace.WriteLine(message);
                    _context.Trace.WriteException(ex);
                    _context.Trace2.WriteError(message);
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

        #endregion

        public void Dispose()
        {
            _restApiRegistry.Dispose();
            _bitbucketAuth.Dispose();
        }
    }
}
