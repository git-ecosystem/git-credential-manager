using System;
using System.Net.Http;
using System.Threading.Tasks;
using GitCredentialManager;
using GitCredentialManager.Authentication.OAuth;
using System.Net.Http.Headers;
using System.Linq;

namespace GitLab
{
    public class GitLabHostProvider : HostProvider
    {
        // https://docs.gitlab.com/ee/integration/oauth_provider.html#authorized-applications
        private static readonly string[] GitLabOAuthScopes =
        {
            "write_repository",
            "read_repository"
        };

        private readonly IGitLabAuthentication _gitLabAuth;

        public GitLabHostProvider(ICommandContext context)
            : this(context, new GitLabAuthentication(context)) { }

        public GitLabHostProvider(ICommandContext context, IGitLabAuthentication gitLabAuth)
            : base(context)
        {
            EnsureArgument.NotNull(gitLabAuth, nameof(gitLabAuth));

            _gitLabAuth = gitLabAuth;
        }

        public override string Id => "gitlab";

        public override string Name => "GitLab";

        public override bool IsSupported(InputArguments input)
        {
            if (input is null)
            {
                return false;
            }

            // We do not support unencrypted HTTP communications to GitLab,
            // but we report `true` here for HTTP so that we can show a helpful
            // error message for the user in `CreateCredentialAsync`.
            if (!StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "http") &&
                !StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "https"))
            {
                return false;
            }

            if (GitLabConstants.IsGitLabDotCom(input.GetRemoteUri()))
            {
                return true;
            }

            // Split port number and hostname from host input argument
            if (!input.TryGetHostAndPort(out string hostName, out _))
            {
                return false;
            }

            string[] domains = hostName.Split(new char[] { '.' });

            // GitLab[.subdomain].domain.tld
            if (domains.Length >= 3 &&
                StringComparer.OrdinalIgnoreCase.Equals(domains[0], "gitlab"))
            {
                return true;
            }

            if (input.WwwAuth.Any(x => x.Contains("realm=\"GitLab\"")))
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
            // not always present https://gitlab.com/gitlab-org/gitlab/-/issues/349464
            return response.Headers.Contains("X-Gitlab-Feature-Category");
        }

        public override async Task<ICredential> GenerateCredentialAsync(InputArguments input)
        {
            ThrowIfDisposed();

            // We should not allow unencrypted communication and should inform the user
            if (StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "http"))
            {
                throw new Trace2Exception(Context.Trace2,
                    "Unencrypted HTTP is not supported for GitHub. Ensure the repository remote URL is using HTTPS.");
            }

            Uri remoteUri = input.GetRemoteUri();

            AuthenticationModes authModes = GetSupportedAuthenticationModes(remoteUri);

            AuthenticationPromptResult promptResult = await _gitLabAuth.GetAuthenticationAsync(remoteUri, input.UserName, authModes);

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
                GitLabConstants.EnvironmentVariables.AuthenticationModes,
                Constants.GitConfiguration.Credential.SectionName, GitLabConstants.GitConfiguration.Credential.AuthenticationModes,
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

            // GitLab.com has well-known supported auth modes
            if (GitLabConstants.IsGitLabDotCom(targetUri))
            {
                return GitLabConstants.DotComAuthenticationModes;
            }

            // Try to detect what auth modes are available for this non-GitLab.com host.
            // Assume that PATs are always available to give at least one option to users!
            var modes = AuthenticationModes.Pat;

            // If there is a configured OAuth client ID (that isn't GitLab.com's client ID)
            // then assume OAuth is possible.
            string oauthClientId = GitLabOAuth2Client.GetClientId(Context.Settings);
            if (!GitLabConstants.IsGitLabDotComClientId(oauthClientId))
            {
                modes |= AuthenticationModes.Browser;
            }
            else
            {
                // Tell the user that they may wish to configure OAuth for this GitLab instance
                Context.Streams.Error.WriteLine(
                    $"warning: missing OAuth configuration for {targetUri.Host} - see {GitLabConstants.HelpUrls.GitLab} for more information");
            }

            // Would like to query password_authentication_enabled_for_git, but can't unless logged in https://gitlab.com/gitlab-org/gitlab/-/issues/349463.
            // For now assume password auth is always available.
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
            // https://docs.gitlab.com/ee/api/oauth2.html#retrieve-the-token-information
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

            // username must be 'oauth2' https://docs.gitlab.com/ee/api/oauth2.html#access-git-over-https-with-access-token
            public string Account => "oauth2";
            public string AccessToken { get; }
            public string RefreshToken { get; }
            string ICredential.Password => AccessToken;
        }

        private async Task<OAuthCredential> GenerateOAuthCredentialAsync(InputArguments input)
        {
            OAuth2TokenResult result = await _gitLabAuth.GetOAuthTokenViaBrowserAsync(input.GetRemoteUri(), GitLabOAuthScopes);
            return new OAuthCredential(result);
        }

        private async Task<OAuthCredential> RefreshOAuthCredentialAsync(InputArguments input, string refreshToken)
        {
            OAuth2TokenResult result = await _gitLabAuth.GetOAuthTokenViaRefresh(input.GetRemoteUri(), refreshToken);
            return new OAuthCredential(result);
        }

        protected override void ReleaseManagedResources()
        {
            _gitLabAuth.Dispose();
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
