// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.Authentication.OAuth;

namespace GitHub
{
    public class GitHubHostProvider : HostProvider
    {
        private static readonly string[] GitHubOAuthScopes =
        {
            GitHubConstants.OAuthScopes.Repo,
            GitHubConstants.OAuthScopes.Gist,
            GitHubConstants.OAuthScopes.Workflow,
        };

        private static readonly string[] GitHubCredentialScopes =
        {
            GitHubConstants.TokenScopes.Gist,
            GitHubConstants.TokenScopes.Repo
        };

        private readonly IGitHubRestApi _gitHubApi;
        private readonly IGitHubAuthentication _gitHubAuth;

        public GitHubHostProvider(ICommandContext context)
            : this(context, new GitHubRestApi(context), new GitHubAuthentication(context)) { }

        public GitHubHostProvider(ICommandContext context, IGitHubRestApi gitHubApi, IGitHubAuthentication gitHubAuth)
            : base(context)
        {
            EnsureArgument.NotNull(gitHubApi, nameof(gitHubApi));
            EnsureArgument.NotNull(gitHubAuth, nameof(gitHubAuth));

            _gitHubApi = gitHubApi;
            _gitHubAuth = gitHubAuth;
        }

        public override string Id => "github";

        public override string Name => "GitHub";

        public override IEnumerable<string> SupportedAuthorityIds => GitHubAuthentication.AuthorityIds;

        public override bool IsSupported(InputArguments input)
        {
            // We do not support unencrypted HTTP communications to GitHub,
            // but we report `true` here for HTTP so that we can show a helpful
            // error message for the user in `CreateCredentialAsync`.
            return input != null &&
                   (StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "http") ||
                    StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "https")) &&
                   (StringComparer.OrdinalIgnoreCase.Equals(input.Host, GitHubConstants.GitHubBaseUrlHost) ||
                    StringComparer.OrdinalIgnoreCase.Equals(input.Host, GitHubConstants.GistBaseUrlHost));
        }

        public override string GetCredentialKey(InputArguments input)
        {
            string url = GetTargetUri(input).AbsoluteUri;

            // Trim trailing slash
            if (url.EndsWith("/"))
            {
                url = url.Substring(0, url.Length - 1);
            }

            return $"git:{url}";
        }

        public override async Task<ICredential> GenerateCredentialAsync(InputArguments input)
        {
            ThrowIfDisposed();

            // We should not allow unencrypted communication and should inform the user
            if (StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "http"))
            {
                throw new Exception("Unencrypted HTTP is not supported for GitHub. Ensure the repository remote URL is using HTTPS.");
            }

            Uri targetUri = GetTargetUri(input);

            AuthenticationModes authModes = await GetSupportedAuthenticationModesAsync(targetUri);

            AuthenticationPromptResult promptResult = await _gitHubAuth.GetAuthenticationAsync(targetUri, authModes);

            switch (promptResult.AuthenticationMode)
            {
                case AuthenticationModes.Basic:
                    return await GeneratePersonalAccessTokenAsync(targetUri, promptResult.BasicCredential);

                case AuthenticationModes.OAuth:
                    return await GenerateOAuthCredentialAsync(targetUri);

                default:
                    throw new ArgumentOutOfRangeException(nameof(promptResult));
            }
        }

        private async Task<ICredential> GenerateOAuthCredentialAsync(Uri targetUri)
        {
            OAuth2TokenResult result = await _gitHubAuth.GetOAuthTokenAsync(targetUri, GitHubOAuthScopes);

            return new GitCredential(Constants.OAuthTokenUserName, result.AccessToken);
        }

        private async Task<ICredential> GeneratePersonalAccessTokenAsync(Uri targetUri, ICredential credentials)
        {
            AuthenticationResult result = await _gitHubApi.CreatePersonalTokenAsync(
                targetUri, credentials, null, GitHubCredentialScopes);

            if (result.Type == GitHubAuthenticationResultType.Success)
            {
                Context.Trace.WriteLine($"Token acquisition for '{targetUri}' succeeded");

                return result.Token;
            }

            if (result.Type == GitHubAuthenticationResultType.TwoFactorApp ||
                result.Type == GitHubAuthenticationResultType.TwoFactorSms)
            {
                bool isSms = result.Type == GitHubAuthenticationResultType.TwoFactorSms;

                string authCode = await _gitHubAuth.GetTwoFactorCodeAsync(targetUri, isSms);

                result = await _gitHubApi.CreatePersonalTokenAsync(targetUri, credentials, authCode, GitHubCredentialScopes);

                if (result.Type == GitHubAuthenticationResultType.Success)
                {
                    Context.Trace.WriteLine($"Token acquisition for '{targetUri}' succeeded.");

                    return result.Token;
                }
            }

            throw new Exception($"Interactive logon for '{targetUri}' failed.");
        }

        internal async Task<AuthenticationModes> GetSupportedAuthenticationModesAsync(Uri targetUri)
        {
            // Check for an explicit override for supported authentication modes
            if (Context.Settings.TryGetSetting(
                GitHubConstants.EnvironmentVariables.AuthenticationModes,
                Constants.GitConfiguration.Credential.SectionName, GitHubConstants.GitConfiguration.Credential.AuthenticationModes,
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

            // GitHub.com should use OAuth authentication only
            if (IsGitHubDotCom(targetUri))
            {
                Context.Trace.WriteLine($"{targetUri} is github.com - authentication schemes: '{GitHubConstants.DotDomAuthenticationModes}'");
                return GitHubConstants.DotDomAuthenticationModes;
            }

            // For GitHub Enterprise we must do some detection of supported modes
            Context.Trace.WriteLine($"{targetUri} is GitHub Enterprise - checking for supporting authentication schemes...");

            try
            {
                GitHubMetaInfo metaInfo = await _gitHubApi.GetMetaInfoAsync(targetUri);

                var modes = AuthenticationModes.None;
                if (metaInfo.VerifiablePasswordAuthentication)
                {
                    modes |= AuthenticationModes.Basic;
                }
                if (Version.TryParse(metaInfo.InstalledVersion, out var version) && version >= GitHubConstants.MinimumEnterpriseOAuthVersion)
                {
                    modes |= AuthenticationModes.OAuth;
                }

                Context.Trace.WriteLine($"GitHub Enterprise instance has version '{metaInfo.InstalledVersion}' and supports authentication schemes: {modes}");
                return modes;
            }
            catch (Exception ex)
            {
                Context.Trace.WriteLine($"Failed to query '{targetUri}' for supported authentication schemes.");
                Context.Trace.WriteException(ex);

                Context.Terminal.WriteLine($"warning: failed to query '{targetUri}' for supported authentication schemes.");
                return AuthenticationModes.Basic | AuthenticationModes.OAuth;
            }
        }

        protected override void ReleaseManagedResources()
        {
            _gitHubApi.Dispose();
            _gitHubAuth.Dispose();
            base.ReleaseManagedResources();
        }

        #region Private Methods

        internal static bool IsGitHubDotCom(Uri targetUri)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(targetUri.Host, GitHubConstants.GitHubBaseUrlHost);
        }

        private static Uri NormalizeUri(Uri targetUri)
        {
            if (targetUri is null)
            {
                throw new ArgumentNullException(nameof(targetUri));
            }

            // Special case for gist.github.com which are git backed repositories under the hood.
            // Credentials for these repositories are the same as the one stored with "github.com"
            if (targetUri.DnsSafeHost.Equals(GitHubConstants.GistBaseUrlHost, StringComparison.OrdinalIgnoreCase))
            {
                return new Uri("https://" + GitHubConstants.GitHubBaseUrlHost);
            }

            return targetUri;
        }

        private static Uri GetTargetUri(InputArguments input)
        {
            Uri uri = new UriBuilder
            {
                Scheme = input.Protocol,
                Host = input.Host,
            }.Uri;

            return NormalizeUri(uri);
        }

        #endregion
    }
}
