using System;
using System.Net.Http;
using System.Threading.Tasks;
using GitCredentialManager;
using GitCredentialManager.Authentication.OAuth;
using System.Net.Http.Headers;

namespace Gitee
{
    public class GiteeHostProvider : HostProvider
    {
        // https://gitee.com/api/v5/oauth_doc#/list-item-2
        private static readonly string[] GiteeOAuthScopes =
        {
            "projects "
        };

        private readonly IGiteeAuthentication _giteeAuth;

        public GiteeHostProvider(ICommandContext context)
            : this(context, new GiteeAuthentication(context)) { }

        public GiteeHostProvider(ICommandContext context, IGiteeAuthentication GiteeAuth)
            : base(context)
        {
            EnsureArgument.NotNull(GiteeAuth, nameof(GiteeAuth));

            _giteeAuth = GiteeAuth;
        }

        public override string Id => "gitee";

        public override string Name => "Gitee";

        public override bool IsSupported(InputArguments input)
        {
            if (input is null)
            {
                return false;
            }

            // We do not support unencrypted HTTP communications to Gitee,
            // but we report `true` here for HTTP so that we can show a helpful
            // error message for the user in `CreateCredentialAsync`.
            if (!StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "http") &&
                !StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "https"))
            {
                return false;
            }

            if (GiteeConstants.IsGiteeDotCom(input.GetRemoteUri()))
            {
                return true;
            }

            // Split port number and hostname from host input argument
            if (!input.TryGetHostAndPort(out string hostName, out _))
            {
                return false;
            }

            string[] domains = hostName.Split(new char[] { '.' });

            // Gitee[.subdomain].domain.tld
            if (domains.Length >= 3 &&
                StringComparer.OrdinalIgnoreCase.Equals(domains[0], "gitee"))
            {
                return true;
            }

            return false;
        }

        public override bool IsSupported(HttpResponseMessage response)
        {
            if (response == null)
            {
                return false;
            }

            // as seen at eg. https://salsa.debian.org/apt-team/apt.git
            return response.Headers.Contains("X-Gitee-Feature-Category");
        }

        public override async Task<ICredential> GenerateCredentialAsync(InputArguments input)
        {
            ThrowIfDisposed();

            // We should not allow unencrypted communication and should inform the user
            if (StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "http"))
            {
                throw new Exception("Unencrypted HTTP is not supported for Gitee. Ensure the repository remote URL is using HTTPS.");
            }

            Uri remoteUri = input.GetRemoteUri();

            AuthenticationModes authModes = GetSupportedAuthenticationModes(remoteUri);

            AuthenticationPromptResult promptResult = await _giteeAuth.GetAuthenticationAsync(remoteUri, input.UserName, authModes);

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
                GiteeConstants.EnvironmentVariables.AuthenticationModes,
                Constants.GitConfiguration.Credential.SectionName, GiteeConstants.GitConfiguration.Credential.AuthenticationModes,
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

            // Gitee.com has well-known supported auth modes
            if (GiteeConstants.IsGiteeDotCom(targetUri))
            {
                return GiteeConstants.DotComAuthenticationModes;
            }

            // Try to detect what auth modes are available for this non-Gitee.com host.
            // Assume that PATs are always available to give at least one option to users!
            var modes = AuthenticationModes.Pat;

            // If there is a configured OAuth client ID (that isn't Gitee.com's client ID)
            // then assume OAuth is possible.
            string oauthClientId = GiteeOAuth2Client.GetClientId(Context.Settings);
            if (!GiteeConstants.IsGiteeDotComClientId(oauthClientId))
            {
                modes |= AuthenticationModes.Browser;
            }
            else
            {
                // Tell the user that they may wish to configure OAuth for this Gitee instance
                Context.Streams.Error.WriteLine(
                    $"warning: missing OAuth configuration for {targetUri.Host} - see {GiteeConstants.HelpUrls.Gitee} for more information");
            }
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
            if (credential?.Account == "oauth2" && await IsOAuthTokenExpired(input.GetRemoteUri(), credential.Password))
            {
                Context.Trace.WriteLine("Removing expired OAuth access token...");
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

        private async Task<bool> IsOAuthTokenExpired(Uri baseUri, string accessToken)
        {
            Uri infoUri = new Uri(baseUri, "/oauth/token/info");
            using (HttpClient httpClient = Context.HttpClientFactory.CreateClient())
            {
                httpClient.Timeout = TimeSpan.FromSeconds(15);
                httpClient.DefaultRequestHeaders.Authorization
                         = new AuthenticationHeaderValue("Bearer", accessToken);
                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync(infoUri);
                    return response.StatusCode == System.Net.HttpStatusCode.Unauthorized;
                }
                catch (Exception e)
                {
                    Context.Terminal.WriteLine($"OAuth token info request failed: {e.Message}");
                    return false;
                }
            }
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
            OAuth2TokenResult result = await _giteeAuth.GetOAuthTokenViaBrowserAsync(input.GetRemoteUri(), GiteeOAuthScopes);
            return new OAuthCredential(result);
        }

        private async Task<OAuthCredential> RefreshOAuthCredentialAsync(InputArguments input, string refreshToken)
        {
            OAuth2TokenResult result = await _giteeAuth.GetOAuthTokenViaRefresh(input.GetRemoteUri(), refreshToken);
            return new OAuthCredential(result);
        }

        protected override void ReleaseManagedResources()
        {
            _giteeAuth.Dispose();
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
