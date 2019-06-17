// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;

namespace GitHub
{
    public class GitHubHostProvider : HostProvider
    {
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

        public override string Name => "GitHub";

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
            // We should not allow unencrypted communication and should inform the user
            if (StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "http"))
            {
                throw new Exception("Unencrypted HTTP is not supported for GitHub. Ensure the repository remote URL is using HTTPS.");
            }

            Uri targetUri = GetTargetUri(input);

            ICredential credentials = await _gitHubAuth.GetCredentialsAsync(targetUri);

            AuthenticationResult result = await _gitHubApi.AcquireTokenAsync(
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

                string authCode = await _gitHubAuth.GetAuthenticationCodeAsync(targetUri, isSms);

                result = await _gitHubApi.AcquireTokenAsync(
                    targetUri, credentials, authCode, GitHubCredentialScopes);

                if (result.Type == GitHubAuthenticationResultType.Success)
                {
                    Context.Trace.WriteLine($"Token acquisition for '{targetUri}' succeeded.");

                    return result.Token;
                }
            }

            throw new Exception($"Interactive logon for '{targetUri}' failed.");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _gitHubApi.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Private Methods

        private static Uri NormalizeUri(Uri targetUri)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));

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
