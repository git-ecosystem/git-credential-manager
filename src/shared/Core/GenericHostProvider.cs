using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager.Authentication;
using GitCredentialManager.Authentication.OAuth;

namespace GitCredentialManager
{
    public class GenericHostProvider : DisposableObject, IHostProvider
    {
        private readonly ICommandContext _context;
        private readonly IBasicAuthentication _basicAuth;
        private readonly IWindowsIntegratedAuthentication _winAuth;
        private readonly IOAuthAuthentication _oauth;

        public GenericHostProvider(ICommandContext context)
            : this(context, new BasicAuthentication(context), new WindowsIntegratedAuthentication(context),
                new OAuthAuthentication(context)) { }

        public GenericHostProvider(ICommandContext context,
                                   IBasicAuthentication basicAuth,
                                   IWindowsIntegratedAuthentication winAuth,
                                   IOAuthAuthentication oauth)
        {
            EnsureArgument.NotNull(context, nameof(context));
            EnsureArgument.NotNull(basicAuth, nameof(basicAuth));
            EnsureArgument.NotNull(winAuth, nameof(winAuth));
            EnsureArgument.NotNull(oauth, nameof(oauth));

            _context = context;
            _basicAuth = basicAuth;
            _winAuth = winAuth;
            _oauth = oauth;
        }

        public string Id => "generic";

        public string Name => "Generic";

        public IEnumerable<string> SupportedAuthorityIds =>
            EnumerableExtensions.ConcatMany(
                BasicAuthentication.AuthorityIds,
                WindowsIntegratedAuthentication.AuthorityIds
            );

        public bool IsSupported(InputArguments input)
        {
            // The generic provider should support all possible protocols (HTTP, HTTPS, SMTP, IMAP, etc)
            return input != null && !string.IsNullOrWhiteSpace(input.Protocol);
        }

        public bool IsSupported(HttpResponseMessage response)
        {
            return false;
        }

        public string GetServiceName(InputArguments input)
        {
            // By default we assume the service name will be the absolute URI based on the
            // input arguments from Git, without any userinfo part.
            return input.GetRemoteUri(includeUser: false).AbsoluteUri.TrimEnd('/');
        }

        public async Task<GetCredentialResult> GetCredentialAsync(InputArguments input)
        {
            // Try and locate an existing credential in the OS credential store
            string service = GetServiceName(input);
            _context.Trace.WriteLine($"Looking for existing credential in store with service={service} account={input.UserName}...");

            ICredential credential = _context.CredentialStore.Get(service, input.UserName);
            if (credential == null)
            {
                _context.Trace.WriteLine("No existing credentials found.");

                // No existing credential was found, create a new one
                _context.Trace.WriteLine("Creating new credential...");
                return await GenerateCredentialAsync(input);
            }
            else
            {
                _context.Trace.WriteLine("Existing credential found.");
            }

            return new GetCredentialResult(credential);
        }

        public Task StoreCredentialAsync(InputArguments input)
        {
            string service = GetServiceName(input);

            // WIA-authentication is signaled to Git as an empty username/password pair
            // and we will get called to 'store' these WIA credentials.
            // We avoid storing empty credentials.
            if (string.IsNullOrWhiteSpace(input.UserName) && string.IsNullOrWhiteSpace(input.Password))
            {
                _context.Trace.WriteLine("Not storing empty credential.");
            }
            else
            {
                // Add or update the credential in the store.
                _context.Trace.WriteLine($"Storing credential with service={service} account={input.UserName}...");
                _context.CredentialStore.AddOrUpdate(service, input.UserName, input.Password);
                _context.Trace.WriteLine("Credential was successfully stored.");
            }

            return Task.CompletedTask;
        }

        public Task EraseCredentialAsync(InputArguments input)
        {
            string service = GetServiceName(input);

            // Try to locate an existing credential
            _context.Trace.WriteLine($"Erasing stored credential in store with service={service} account={input.UserName}...");
            if (_context.CredentialStore.Remove(service, input.UserName))
            {
                _context.Trace.WriteLine("Credential was successfully erased.");
            }
            else
            {
                _context.Trace.WriteLine("No credential was erased.");
            }

            return Task.CompletedTask;
        }

        public async Task<GetCredentialResult> GenerateCredentialAsync(InputArguments input)
        {
            ThrowIfDisposed();

            // We only want to *warn* about HTTP remotes for the generic provider because it supports all protocols
            // and, historically, we never blocked HTTP remotes in this provider.
            // The user can always set the 'GCM_ALLOW_UNSAFE' setting to silence the warning.
            if (!_context.Settings.AllowUnsafeRemotes &&
                StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "http"))
            {
                _context.Streams.Error.WriteLine(
                    "warning: use of unencrypted HTTP remote URLs is not recommended; " +
                    $"see {Constants.HelpUrls.GcmUnsafeRemotes} for more information.");
            }

            Uri uri = input.GetRemoteUri();

            // Determine the if the host supports Windows Integration Authentication (WIA) or OAuth
            if (!StringComparer.OrdinalIgnoreCase.Equals(uri.Scheme, "http") &&
                !StringComparer.OrdinalIgnoreCase.Equals(uri.Scheme, "https"))
            {
                // Cannot check WIA or OAuth support for non-HTTP based protocols
            }
            // Check for an OAuth configuration for this remote
            else if (GenericOAuthConfig.TryGet(_context.Trace, _context.Settings, input, out GenericOAuthConfig oauthConfig))
            {
                _context.Trace.WriteLine($"Found generic OAuth configuration for '{uri}':");
                _context.Trace.WriteLine($"\tAuthzEndpoint   = {oauthConfig.Endpoints.AuthorizationEndpoint}");
                _context.Trace.WriteLine($"\tTokenEndpoint   = {oauthConfig.Endpoints.TokenEndpoint}");
                _context.Trace.WriteLine($"\tDeviceEndpoint  = {oauthConfig.Endpoints.DeviceAuthorizationEndpoint}");
                _context.Trace.WriteLine($"\tClientId        = {oauthConfig.ClientId}");
                _context.Trace.WriteLine($"\tClientSecret    = {oauthConfig.ClientSecret}");
                _context.Trace.WriteLine($"\tRedirectUri     = {oauthConfig.RedirectUri}");
                _context.Trace.WriteLine($"\tScopes          = [{string.Join(", ", oauthConfig.Scopes)}]");
                _context.Trace.WriteLine($"\tUseAuthHeader   = {oauthConfig.UseAuthHeader}");
                _context.Trace.WriteLine($"\tDefaultUserName = {oauthConfig.DefaultUserName}");

                return new  GetCredentialResult(
                    await GetOAuthAccessToken(uri, input.UserName, oauthConfig, _context.Trace2)
                );
            }
            // Try detecting WIA for this remote, if permitted
            else if (IsWindowsAuthAllowed)
            {
                if (PlatformUtils.IsWindows())
                {
                    _context.Trace.WriteLine($"Checking host '{uri.AbsoluteUri}' for Windows Integrated Authentication...");
                    var supportedWiaTypes = await _winAuth.GetAuthenticationTypesAsync(uri);
                    bool isWiaSupported = supportedWiaTypes != WindowsAuthenticationTypes.None;

                    if (!isWiaSupported)
                    {
                        _context.Trace.WriteLine("Host does not support WIA.");
                    }
                    else
                    {
                        _context.Trace.WriteLine("Host supports WIA.");

                        var additionalProps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                        // Has Git suppressed its own built-in NTLM authentication support?
                        if (input.TryGetArgument(Constants.CredentialProtocol.NtlmKey, out string ntlmArg) &&
                            StringComparer.OrdinalIgnoreCase.Equals(Constants.CredentialProtocol.NtlmSuppressed, ntlmArg))
                        {
                            _context.Trace.WriteLine("NTLM support has been suppressed by Git - showing warning.");

                            // Show a warning that NTLM authentication will not work without Git's built-in support
                            // and ask the user what they want to do about it.
                            NtlmSupport ntlmSupport = await _winAuth.AskEnableNtlmAsync(uri);
                            switch (ntlmSupport)
                            {
                                case NtlmSupport.Once:
                                    _context.Trace.WriteLine("Enabling NTLM support just once.");
                                    additionalProps[Constants.CredentialProtocol.NtlmKey] =
                                        Constants.CredentialProtocol.NtlmAllow;
                                    break;

                                case NtlmSupport.Always:
                                    _context.Trace.WriteLine($"Enabling NTLM support for {uri}.");
                                    additionalProps[Constants.CredentialProtocol.NtlmKey] =
                                        Constants.CredentialProtocol.NtlmAllow;
                                    EnableNtlmSupport(uri);
                                    break;

                                default:
                                    _context.Trace.WriteLine("User declined to enable NTLM support. Showing basic auth prompt.");
                                    return new GetCredentialResult(
                                        await _basicAuth.GetCredentialsAsync(uri.AbsoluteUri, null)
                                    );
                            }
                        }

                        // WIA is signaled to Git using an empty username/password
                        _context.Trace.WriteLine("Returning empty username/password to trigger current user auth with WIA.");
                        ICredential creds = new GitCredential(string.Empty, string.Empty);
                        return new GetCredentialResult(creds)
                        {
                            AdditionalProperties = additionalProps
                        };
                    }
                }
                else
                {
                    string osType = PlatformUtils.GetPlatformInformation(_context.Trace2).OperatingSystemType;
                    _context.Trace.WriteLine($"Skipping check for Windows Integrated Authentication on {osType}.");
                }
            }
            else
            {
                _context.Trace.WriteLine("Windows Integrated Authentication detection has been disabled.");
            }

            // Use basic authentication
            _context.Trace.WriteLine("Prompting for basic credentials...");
            return new GetCredentialResult(
                await _basicAuth.GetCredentialsAsync(uri.AbsoluteUri, input.UserName)
            );
        }

        private void EnableNtlmSupport(Uri uri)
        {
            string url = uri.AbsoluteUri.TrimEnd('/');
            IGitConfiguration config = _context.Git.GetConfiguration();
            string key = $"{Constants.GitConfiguration.Http.SectionName}.{url}.{Constants.GitConfiguration.Http.AllowNtlmAuth}";

            try
            {
                config.Set(GitConfigurationLevel.Global, key, "true");
            }
            catch (Exception ex)
            {
                _context.Trace.WriteLine($"Failed to set Git configuration to enable NTLM support for {uri}");
                _context.Trace.WriteException(ex);
            }
        }

        private async Task<ICredential> GetOAuthAccessToken(Uri remoteUri, string userName, GenericOAuthConfig config, ITrace2 trace2)
        {
            // TODO: Determined user info from a webcall? ID token? Need OIDC support
            string oauthUser = userName ?? config.DefaultUserName;

            var client = new OAuth2Client(
                HttpClient,
                config.Endpoints,
                config.ClientId,
                trace2,
                config.RedirectUri,
                config.ClientSecret,
                config.UseAuthHeader);

            //
            // Prepend "refresh_token" to the hostname to get a (hopefully) unique service name that
            // doesn't clash with an existing credential service.
            //
            // Appending "/refresh_token" to the end of the remote URI may not always result in a unique
            // service because users may set credential.useHttpPath and include "/refresh_token" as a
            // path name.
            //
            string refreshService = new UriBuilder(remoteUri) { Host = $"refresh_token.{remoteUri.Host}" }
                .Uri.AbsoluteUri.TrimEnd('/');

            // Try to use a refresh token if we have one
            ICredential refreshToken = _context.CredentialStore.Get(refreshService, userName);
            if (refreshToken != null)
            {
                try
                {
                    var refreshResult = await client.GetTokenByRefreshTokenAsync(refreshToken.Password, CancellationToken.None);

                    // Store new refresh token if we have been given one
                    if (!string.IsNullOrWhiteSpace(refreshResult.RefreshToken))
                    {
                        _context.CredentialStore.AddOrUpdate(refreshService, refreshToken.Account, refreshResult.RefreshToken);
                    }

                    // Return the new access token
                    return new GitCredential(oauthUser,refreshResult.AccessToken);
                }
                catch (OAuth2Exception ex)
                {
                    // Failed to use refresh token. It may have expired or been revoked.
                    // Fall through to an interactive OAuth flow.
                    _context.Trace.WriteLine("Failed to use refresh token.");
                    _context.Trace.WriteException(ex);
                }
            }

            // Determine which interactive OAuth mode to use. Start by checking for mode preference in config
            var supportedModes = OAuthAuthenticationModes.All;
            if (_context.Settings.TryGetSetting(
                    Constants.EnvironmentVariables.OAuthAuthenticationModes,
                    Constants.GitConfiguration.Credential.SectionName,
                    Constants.GitConfiguration.Credential.OAuthAuthenticationModes,
                    out string authModesStr))
            {
                if (Enum.TryParse(authModesStr, true, out supportedModes) && supportedModes != OAuthAuthenticationModes.None)
                {
                    _context.Trace.WriteLine($"Supported authentication modes override present: {supportedModes}");
                }
                else
                {
                    _context.Trace.WriteLine($"Invalid value for supported authentication modes override setting: '{authModesStr}'");
                }
            }

            // If the server doesn't support device code we need to remove it as an option here
            if (!config.SupportsDeviceCode)
            {
                supportedModes &= ~OAuthAuthenticationModes.DeviceCode;
            }

            // Prompt the user to select a mode
            OAuthAuthenticationModes mode = await _oauth.GetAuthenticationModeAsync(remoteUri.ToString(), supportedModes);

            OAuth2TokenResult tokenResult;
            switch (mode)
            {
                case OAuthAuthenticationModes.Browser:
                    tokenResult = await _oauth.GetTokenByBrowserAsync(client, config.Scopes);
                    break;

                case OAuthAuthenticationModes.DeviceCode:
                    tokenResult = await _oauth.GetTokenByDeviceCodeAsync(client, config.Scopes);
                    break;

                default:
                    throw new Trace2Exception(_context.Trace2, "No authentication mode selected!");
            }

            // Store the refresh token if we have one
            if (!string.IsNullOrWhiteSpace(tokenResult.RefreshToken))
            {
                _context.CredentialStore.AddOrUpdate(refreshService, oauthUser, tokenResult.RefreshToken);
            }

            return new GitCredential(oauthUser, tokenResult.AccessToken);
        }

        /// <summary>
        /// Check if the user permits checking for Windows Integrated Authentication.
        /// </summary>
        /// <remarks>
        /// Checks the explicit 'GCM_ALLOW_WINDOWSAUTH' setting and also the legacy 'GCM_AUTHORITY' setting iif equal to "basic".
        /// </remarks>
        private bool IsWindowsAuthAllowed
        {
            get
            {
                if (_context.Settings.IsWindowsIntegratedAuthenticationEnabled)
                {
                    /* COMPAT: In the old GCM one workaround for common authentication problems was to specify "basic" as the authority
                     *         which prevents any smart detection of provider or NTLM etc, allowing the user a chance to manually enter
                     *         a username/password or PAT.
                     *
                     *         We take this old setting into account to ensure a good migration experience.
                     */
                    return !BasicAuthentication.AuthorityIds.Contains(_context.Settings.LegacyAuthorityOverride, StringComparer.OrdinalIgnoreCase);
                }

                return false;
            }
        }

        private HttpClient _httpClient;
        private HttpClient HttpClient => _httpClient ?? (_httpClient = _context.HttpClientFactory.CreateClient());

        protected override void ReleaseManagedResources()
        {
            _winAuth.Dispose();
            _httpClient?.Dispose();
            base.ReleaseManagedResources();
        }
    }
}
