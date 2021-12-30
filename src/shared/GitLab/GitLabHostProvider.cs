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

        private readonly IGitLabAuthentication _GitLabAuth;

        public GitLabHostProvider(ICommandContext context)
            : this(context, new GitLabAuthentication(context)) { }

        public GitLabHostProvider(ICommandContext context, IGitLabAuthentication GitLabAuth)
            : base(context)
        {
            EnsureArgument.NotNull(GitLabAuth, nameof(GitLabAuth));

            _GitLabAuth = GitLabAuth;
        }

        public override string Id => "GitLab";

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

            // Split port number and hostname from host input argument
            if (!input.TryGetHostAndPort(out string hostName, out _))
            {
                return false;
            }

            if (GitLabConstants.GitLabApplicationsByHost.ContainsKey(hostName))
            {
                return true;
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

            AuthenticationPromptResult promptResult = _GitLabAuth.GetAuthentication(remoteUri, input.UserName, authModes);

            switch (promptResult.AuthenticationMode)
            {
                case AuthenticationModes.Basic:
                    return promptResult.Credential;

                case AuthenticationModes.Browser:
                    return await GenerateOAuthCredentialAsync(remoteUri);

                case AuthenticationModes.Pat:
                    string token = promptResult.Credential.Password;

                    // GitLab accepts any username https://gitlab.com/gitlab-org/gitlab/-/issues/212953
                    string userName = promptResult.Credential.Account ?? "pat";

                    return new GitCredential(userName, token);

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

            AuthenticationModes modes = AuthenticationModes.Basic | AuthenticationModes.Pat;

            try
            {
                GitLabOAuth2Client.GetClientId(Context.Settings, targetUri);
            }
            catch (Exception e)
            {
                Context.Streams.Error.WriteLine(e.Message);
                return modes;
            }
            modes |= AuthenticationModes.Browser;
            return modes;
        }

        private async Task<GitCredential> GenerateOAuthCredentialAsync(Uri targetUri)
        {
            OAuth2TokenResult result = await _GitLabAuth.GetOAuthTokenViaBrowserAsync(targetUri, GitLabOAuthScopes);

            // username oauth2 https://gitlab.com/gitlab-org/gitlab/-/issues/349461
            return new GitCredential("oauth2", result.AccessToken);
        }

        protected override void ReleaseManagedResources()
        {
            _GitLabAuth.Dispose();
            base.ReleaseManagedResources();
        }
    }
}
