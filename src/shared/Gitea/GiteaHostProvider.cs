using System;
using System.Linq;  
using System.Net.Http;
using System.Threading.Tasks;
using GitCredentialManager;
using GitCredentialManager.Authentication.OAuth;
using System.Net.Http.Headers;

namespace Gitea
{
    public class GiteaHostProvider : HostProvider
    {
        private static readonly string[] GiteaOAuthScopes =
        {
        };

        private readonly IGiteaAuthentication _giteaAuth;

        public GiteaHostProvider(ICommandContext context)
            : this(context, new GiteaAuthentication(context)) { }

        public GiteaHostProvider(ICommandContext context, IGiteaAuthentication giteaAuth)
            : base(context)
        {
            EnsureArgument.NotNull(giteaAuth, nameof(giteaAuth));

            _giteaAuth = giteaAuth;
        }

        public override string Id => "gitea";

        public override string Name => "Gitea";

        public override bool IsSupported(InputArguments input)
        {
            if (input is null)
            {
                return false;
            }

            // We do not support unencrypted HTTP communications to Gitea,
            // but we report `true` here for HTTP so that we can show a helpful
            // error message for the user in `CreateCredentialAsync`.
            if (!StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "http") &&
                !StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "https"))
            {
                return false;
            }

            // Split port number and hostname from host input argument
            if (!input.TryGetHostAndPort(out string hostName, out _))
            {
                return false;
            }

            string[] domains = hostName.Split(new char[] { '.' });

            // Gitea[.subdomain].domain.tld
            if (domains.Length >= 3 &&
                StringComparer.OrdinalIgnoreCase.Equals(domains[0], "gitea"))
            {
                return true;
            }

            return false;
        }

        public override bool IsSupported(HttpResponseMessage response) =>
         response?.Headers.Any(pair => pair.Key == "Set-Cookie" && pair.Value.Any(x => x.Contains("i_like_gitea="))) ?? false;

        public override async Task<ICredential> GenerateCredentialAsync(InputArguments input)
        {
            ThrowIfDisposed();

            Uri remoteUri = input.GetRemoteUri();

            AuthenticationModes authModes = GetSupportedAuthenticationModes(remoteUri);

            AuthenticationPromptResult promptResult = _giteaAuth.GetAuthentication(remoteUri, input.UserName, authModes);

            switch (promptResult.AuthenticationMode)
            {
                case AuthenticationModes.Basic:
                case AuthenticationModes.Pat:
                    return promptResult.Credential;

                case AuthenticationModes.Browser:
                    return await GenerateOAuthCredentialAsync(input);

                default:
                    throw new ArgumentOutOfRangeException(nameof(promptResult));
            }
        }

        internal AuthenticationModes GetSupportedAuthenticationModes(Uri targetUri)
        {
            // Check for an explicit override for supported authentication modes
            if (Context.Settings.TryGetSetting(
                GiteaConstants.EnvironmentVariables.AuthenticationModes,
                Constants.GitConfiguration.Credential.SectionName, GiteaConstants.GitConfiguration.Credential.AuthenticationModes,
                out string authModesStr))
            {
                if (Enum.TryParse(authModesStr, true, out AuthenticationModes authModes) && authModes != AuthenticationModes.None)
                {
                    Context.Trace.WriteLine($"Supported authentication modes override present: {authModes}");
                    return authModes;
                }
                else
                {
                    Context.Trace.WriteLine($"Invalid value for supported authentication modes override setting: '{authModesStr}'");
                }
            }

            // Try to detect what auth modes are available for this non-Gitea.com host.
            // Assume that PATs are always available to give at least one option to users!
            var modes = AuthenticationModes.Pat;

            // If there is a configured OAuth client ID
            // then assume OAuth is possible.
            string oauthClientId = GiteaOAuth2Client.GetClientId(Context.Settings);
            if (oauthClientId != null) {
                modes |= AuthenticationModes.Browser;
            } else {
                // Tell the user that they may wish to configure OAuth for this Gitea instance
                Context.Streams.Error.WriteLine(
                    $"warning: missing OAuth configuration for {targetUri.Host} - see {GiteaConstants.HelpUrls.Gitea} for more information");
            }

            // assume password auth is always available.
            bool supportsBasic = true;
            if (supportsBasic)
            {
                modes |= AuthenticationModes.Basic;
            }

            return modes;
        }

        // <remarks>Stores OAuth tokens as a side effect</remarks>
        public override async Task<ICredential> GetCredentialAsync(InputArguments input)
        {
            string service = GetServiceName(input);
            ICredential credential = Context.CredentialStore.Get(service, input.UserName);
            if (credential?.Account == "oauth2" && IsOAuthTokenExpired(input.GetRemoteUri(), credential.Password))
            {
                Context.Trace.WriteLine("Removing (possibly) expired OAuth access token...");
                Context.CredentialStore.Remove(service, credential.Account);
                credential = null;
            }

            if (credential != null)
            {
                return credential;
            }

            string refreshService = GetRefreshTokenServiceName(input);
            string refreshToken = Context.CredentialStore.Get(refreshService, input.UserName)?.Password;
            if (refreshToken != null)
            {
                Context.Trace.WriteLine("Refreshing OAuth token...");
                try
                {
                    credential = await RefreshOAuthCredentialAsync(input, refreshToken);
                }
                catch (Exception e)
                {
                    Context.Terminal.WriteLine($"OAuth token refresh failed: {e.Message}");
                }
            }

            credential ??= await GenerateCredentialAsync(input);

            if (credential is OAuthCredential oAuthCredential)
            {
                Context.Trace.WriteLine("Pre-emptively storing OAuth access and refresh tokens...");
                // freshly-generated OAuth credential
                // store credential, since we know it to be valid (whereas Git will only store credential if git push succeeds)
                Context.CredentialStore.AddOrUpdate(service, oAuthCredential.Account, oAuthCredential.AccessToken);
                // store refresh token under a separate service
                Context.CredentialStore.AddOrUpdate(refreshService, oAuthCredential.Account, oAuthCredential.RefreshToken);
            }
            return credential;
        }

        private bool IsOAuthTokenExpired(Uri baseUri, string accessToken)
        {
            return true;
        }

        internal class OAuthCredential : ICredential
        {
            public OAuthCredential(OAuth2TokenResult oAuth2TokenResult)
            {
                AccessToken = oAuth2TokenResult.AccessToken;
                RefreshToken = oAuth2TokenResult.RefreshToken;
            }

            public string Account => "oauth2";
            public string AccessToken { get; }
            public string RefreshToken { get; }
            string ICredential.Password => AccessToken;
        }

        private async Task<OAuthCredential> GenerateOAuthCredentialAsync(InputArguments input)
        {
            OAuth2TokenResult result = await _giteaAuth.GetOAuthTokenViaBrowserAsync(input.GetRemoteUri(), GiteaOAuthScopes);
            return new OAuthCredential(result);
        }

        private async Task<OAuthCredential> RefreshOAuthCredentialAsync(InputArguments input, string refreshToken)
        {
            OAuth2TokenResult result = await _giteaAuth.GetOAuthTokenViaRefresh(input.GetRemoteUri(), refreshToken);
            return new OAuthCredential(result);
        }

        protected override void ReleaseManagedResources()
        {
            _giteaAuth.Dispose();
            base.ReleaseManagedResources();
        }

        private string GetRefreshTokenServiceName(InputArguments input)
        {
            var builder = new UriBuilder(GetServiceName(input));
            builder.Host = "oauth-refresh-token." + builder.Host;
            return builder.Uri.ToString();
        }

        public override Task EraseCredentialAsync(InputArguments input)
        {
            // delete any refresh token too
            Context.CredentialStore.Remove(GetRefreshTokenServiceName(input), "oauth2");
            return base.EraseCredentialAsync(input);
        }
    }
}
