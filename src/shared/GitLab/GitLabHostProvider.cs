using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using GitCredentialManager;
using GitCredentialManager.Authentication.OAuth;

namespace GitLab
{
    public class GitLabHostProvider : HostProvider
    {
        // https://docs.gitlab.com/ee/integration/oauth_provider.html#authorized-applications
        private static readonly string[] GitLabOAuthScopes =
        {
            "write_repository",
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

        public override string GetServiceName(InputArguments input)
        {
            var baseUri = new Uri(base.GetServiceName(input));

            string url = baseUri.AbsoluteUri;

            // Trim trailing slash
            return url.TrimEnd('/');
        }

        public override async Task<ICredential> GenerateCredentialAsync(InputArguments input)
        {
            ThrowIfDisposed();

            // We should not allow unencrypted communication and should inform the user
            if (StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "http"))
            {
                throw new Exception("Unencrypted HTTP is not supported for GitLab. Ensure the repository remote URL is using HTTPS.");
            }

            Uri remoteUri = input.GetRemoteUri();

            AuthenticationModes authModes = GetSupportedAuthenticationModes(remoteUri);

            AuthenticationPromptResult promptResult = _gitLabAuth.GetAuthentication(remoteUri, input.UserName, authModes);

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

            if (GitLabConstants.IsGitLabDotCom(targetUri))
            {
                return AuthenticationModes.All;
            }

            Context.Streams.Error.WriteLine($"Missing OAuth configuration for {targetUri.Host}, see https://github.com/GitCredentialManager/git-credential-manager/blob/main/docs/gitlab.md.");
            // Would like to query password_authentication_enabled_for_git, but can't unless logged in https://gitlab.com/gitlab-org/gitlab/-/issues/349463
            return AuthenticationModes.Basic | AuthenticationModes.Pat;
        }

        public override async Task<ICredential> GetCredentialAsync(InputArguments input)
        {
            ICredential credential = await base.GetCredentialAsync(input);
            if (credential.Account == "oauth2")
            {
                // cast succeeds if and only if credential is freshly generated (not retrieved)
                OAuthCredential oAuthCredential = credential as OAuthCredential;
                if (oAuthCredential == null)
                {
                    // retrieved OAuth credential may have expired, so refresh
                    try
                    {
                        oAuthCredential = await RefreshOAuthCredentialAsync(input);
                    }
                    catch (Exception e)
                    {
                        Context.Terminal.WriteLine($"OAuth token refresh failed: {e.Message}");
                        return credential;
                    }
                }
                // store refresh token under a separate service
                Context.Trace.WriteLine("Storing refresh token...");
                Context.CredentialStore.AddOrUpdate(GetRefreshTokenServiceName(input.GetRemoteUri()), "oauth2", oAuthCredential.RefreshToken);
                return oAuthCredential;
            }

            return credential;
        }

        internal class OAuthCredential : GitCredential
        {
            // username must be oauth2 https://gitlab.com/gitlab-org/gitlab/-/issues/349461
            public OAuthCredential(OAuth2TokenResult oAuth2TokenResult) : base("oauth2", oAuth2TokenResult.AccessToken)
            {
                RefreshToken = oAuth2TokenResult.RefreshToken;
            }

            public string RefreshToken { get; }
        }

        private async Task<OAuthCredential> GenerateOAuthCredentialAsync(InputArguments input)
        {
            OAuth2TokenResult result = await _gitLabAuth.GetOAuthTokenViaBrowserAsync(input.GetRemoteUri(), GitLabOAuthScopes);
            return new OAuthCredential(result);
        }

        private async Task<OAuthCredential> RefreshOAuthCredentialAsync(InputArguments input)
        {
            // retrieve refresh token stored under separate service
            Context.Trace.WriteLine($"Checking for stored refresh token...");
            string refreshTokenServiceName = GetRefreshTokenServiceName(input.GetRemoteUri());
            string refreshToken = Context.CredentialStore.Get(refreshTokenServiceName, "oauth2").Password;
            if (refreshToken == null)
            {
                throw new InvalidOperationException("No stored refresh token");
            }
            OAuth2TokenResult result = await _gitLabAuth.GetOAuthTokenViaRefresh(input.GetRemoteUri(), refreshToken);
            return new OAuthCredential(result);
        }

        protected override void ReleaseManagedResources()
        {
            _gitLabAuth.Dispose();
            base.ReleaseManagedResources();
        }

        private static string GetRefreshTokenServiceName(Uri baseUri)
        {
            return new Uri(baseUri.WithoutUserInfo(), "/refresh_token").AbsoluteUri;
        }
    }
}
