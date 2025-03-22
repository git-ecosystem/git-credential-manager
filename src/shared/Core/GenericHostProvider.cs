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
    public class GenericHostProvider : HostProvider
    {
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
            : base(context)
        {
            EnsureArgument.NotNull(basicAuth, nameof(basicAuth));
            EnsureArgument.NotNull(winAuth, nameof(winAuth));
            EnsureArgument.NotNull(oauth, nameof(oauth));

            _basicAuth = basicAuth;
            _winAuth = winAuth;
            _oauth = oauth;
        }

        public override string Id => "generic";

        public override string Name => "Generic";

        public override IEnumerable<string> SupportedAuthorityIds =>
            EnumerableExtensions.ConcatMany(
                BasicAuthentication.AuthorityIds,
                WindowsIntegratedAuthentication.AuthorityIds
            );

        public override bool IsSupported(InputArguments input)
        {
            // The generic provider should support all possible protocols (HTTP, HTTPS, SMTP, IMAP, etc)
            return input != null && !string.IsNullOrWhiteSpace(input.Protocol);
        }

        public override async Task<ICredential> GenerateCredentialAsync(InputArguments input)
        {
            ThrowIfDisposed();

            Uri uri = input.GetRemoteUri();

            // Determine the if the host supports Windows Integration Authentication (WIA) or OAuth
            if (!StringComparer.OrdinalIgnoreCase.Equals(uri.Scheme, "http") &&
                !StringComparer.OrdinalIgnoreCase.Equals(uri.Scheme, "https"))
            {
                // Cannot check WIA or OAuth support for non-HTTP based protocols
            }
            // Check for an OAuth configuration for this remote
            else if (GenericOAuthConfig.TryGet(Context.Trace, Context.Settings, input, out GenericOAuthConfig oauthConfig))
            {
                Context.Trace.WriteLine($"Found generic OAuth configuration for '{uri}':");
                Context.Trace.WriteLine($"\tAuthzEndpoint   = {oauthConfig.Endpoints.AuthorizationEndpoint}");
                Context.Trace.WriteLine($"\tTokenEndpoint   = {oauthConfig.Endpoints.TokenEndpoint}");
                Context.Trace.WriteLine($"\tDeviceEndpoint  = {oauthConfig.Endpoints.DeviceAuthorizationEndpoint}");
                Context.Trace.WriteLine($"\tClientId        = {oauthConfig.ClientId}");
                Context.Trace.WriteLine($"\tClientSecret    = {oauthConfig.ClientSecret}");
                Context.Trace.WriteLine($"\tRedirectUri     = {oauthConfig.RedirectUri}");
                Context.Trace.WriteLine($"\tScopes          = [{string.Join(", ", oauthConfig.Scopes)}]");
                Context.Trace.WriteLine($"\tUseAuthHeader   = {oauthConfig.UseAuthHeader}");
                Context.Trace.WriteLine($"\tDefaultUserName = {oauthConfig.DefaultUserName}");

                return await GetOAuthAccessToken(uri, input.UserName, oauthConfig, Context.Trace2);
            }
            // Try detecting WIA for this remote, if permitted
            else if (IsWindowsAuthAllowed)
            {
                if (PlatformUtils.IsWindows())
                {
                    Context.Trace.WriteLine($"Checking host '{uri.AbsoluteUri}' for Windows Integrated Authentication...");
                    bool isWiaSupported = await _winAuth.GetIsSupportedAsync(uri);

                    if (!isWiaSupported)
                    {
                        Context.Trace.WriteLine("Host does not support WIA.");
                    }
                    else
                    {
                        Context.Trace.WriteLine("Host supports WIA - generating empty credential...");

                        // WIA is signaled to Git using an empty username/password
                        return new GitCredential(string.Empty, string.Empty);
                    }
                }
                else
                {
                    string osType = PlatformUtils.GetPlatformInformation(Context.Trace2).OperatingSystemType;
                    Context.Trace.WriteLine($"Skipping check for Windows Integrated Authentication on {osType}.");
                }
            }
            else
            {
                Context.Trace.WriteLine("Windows Integrated Authentication detection has been disabled.");
            }

            // Use basic authentication
            Context.Trace.WriteLine("Prompting for basic credentials...");
            return await _basicAuth.GetCredentialsAsync(uri.AbsoluteUri, input.UserName);
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
            ICredential refreshToken = Context.CredentialStore.Get(refreshService, userName);
            if (refreshToken != null)
            {
                try
                {
                    var refreshResult = await client.GetTokenByRefreshTokenAsync(refreshToken.Password, CancellationToken.None);

                    // Store new refresh token if we have been given one
                    if (!string.IsNullOrWhiteSpace(refreshResult.RefreshToken))
                    {
                        Context.CredentialStore.AddOrUpdate(refreshService, refreshToken.Account, refreshToken.Password);
                    }

                    // Return the new access token
                    return new GitCredential(oauthUser,refreshResult.AccessToken);
                }
                catch (OAuth2Exception ex)
                {
                    // Failed to use refresh token. It may have expired or been revoked.
                    // Fall through to an interactive OAuth flow.
                    Context.Trace.WriteLine("Failed to use refresh token.");
                    Context.Trace.WriteException(ex);
                }
            }

            // Determine which interactive OAuth mode to use. Start by checking for mode preference in config
            var supportedModes = OAuthAuthenticationModes.All;
            if (Context.Settings.TryGetSetting(
                    Constants.EnvironmentVariables.OAuthAuthenticationModes,
                    Constants.GitConfiguration.Credential.SectionName,
                    Constants.GitConfiguration.Credential.OAuthAuthenticationModes,
                    out string authModesStr))
            {
                if (Enum.TryParse(authModesStr, true, out supportedModes) && supportedModes != OAuthAuthenticationModes.None)
                {
                    Context.Trace.WriteLine($"Supported authentication modes override present: {supportedModes}");
                }
                else
                {
                    Context.Trace.WriteLine($"Invalid value for supported authentication modes override setting: '{authModesStr}'");
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
                    throw new Trace2Exception(Context.Trace2, "No authentication mode selected!");
            }

            // Store the refresh token if we have one
            if (!string.IsNullOrWhiteSpace(tokenResult.RefreshToken))
            {
                Context.CredentialStore.AddOrUpdate(refreshService, oauthUser, tokenResult.RefreshToken);
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
                if (Context.Settings.IsWindowsIntegratedAuthenticationEnabled)
                {
                    /* COMPAT: In the old GCM one workaround for common authentication problems was to specify "basic" as the authority
                     *         which prevents any smart detection of provider or NTLM etc, allowing the user a chance to manually enter
                     *         a username/password or PAT.
                     *
                     *         We take this old setting into account to ensure a good migration experience.
                     */
                    return !BasicAuthentication.AuthorityIds.Contains(Context.Settings.LegacyAuthorityOverride, StringComparer.OrdinalIgnoreCase);
                }

                return false;
            }
        }

        private HttpClient _httpClient;
        private HttpClient HttpClient => _httpClient ?? (_httpClient = Context.HttpClientFactory.CreateClient());

        protected override void ReleaseManagedResources()
        {
            _winAuth.Dispose();
            _httpClient?.Dispose();
            base.ReleaseManagedResources();
        }
    }
}
