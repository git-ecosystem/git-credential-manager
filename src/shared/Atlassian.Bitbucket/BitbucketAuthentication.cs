// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.Authentication;
using Microsoft.Git.CredentialManager.Authentication.OAuth;

namespace Atlassian.Bitbucket
{

    [Flags]
    public enum AuthenticationModes
    {
        None = 0,
        Basic = 1,
        OAuth = 1 << 1,
        //Pat = 1 << 2,

        All = Basic | OAuth //| Pat
    }
    public interface IBitbucketAuthentication : IDisposable
    {
        Task<ICredential> GetBasicCredentialsAsync(Uri targetUri, string userName);
        Task<bool> ShowOAuthRequiredPromptAsync();
        Task<OAuth2TokenResult> CreateOAuthCredentialsAsync(Uri targetUri);
        Task<OAuth2TokenResult> RefreshOAuthCredentialsAsync(string refreshToken);
    }

    public class BitbucketAuthentication : AuthenticationBase, IBitbucketAuthentication
    {
        public static readonly string[] AuthorityIds =
        {
            "bitbucket",
        };

        private static readonly string[] Scopes =
        {
            BitbucketConstants.OAuthScopes.RepositoryWrite,
            BitbucketConstants.OAuthScopes.Account,
        };

        public BitbucketAuthentication(ICommandContext context)
            : base(context) { }

        #region IBitbucketAuthentication

        public async Task<ICredential> GetBasicCredentialsAsync(Uri targetUri, string userName)
        {
            ThrowIfUserInteractionDisabled();

            string password;

            // Shell out to the UI helper and show the Bitbucket u/p prompt
            if (Context.SessionManager.IsDesktopSession && TryFindHelperExecutablePath(out string helperPath))
            {
                var cmdArgs = new StringBuilder("userpass");
                if (!string.IsNullOrWhiteSpace(userName))
                {
                    cmdArgs.AppendFormat(" --username {0}", QuoteCmdArg(userName));
                }

                IDictionary<string, string> output = await InvokeHelperAsync(helperPath, cmdArgs.ToString());

                if (!output.TryGetValue("username", out userName))
                {
                    throw new Exception("Missing username in response");
                }

                if (!output.TryGetValue("password", out password))
                {
                    throw new Exception("Missing password in response");
                }

                return new GitCredential(userName, password);
            }
            else
            {
                ThrowIfTerminalPromptsDisabled();

                Context.Terminal.WriteLine("Enter Bitbucket credentials for '{0}'...", targetUri);

                if (!string.IsNullOrWhiteSpace(userName))
                {
                    // Don't need to prompt for the username if it has been specified already
                    Context.Terminal.WriteLine("Username: {0}", userName);
                }
                else
                {
                    // Prompt for username
                    userName = Context.Terminal.Prompt("Username");
                }

                // Prompt for password
                password = Context.Terminal.PromptSecret("Password");

                return new GitCredential(userName, password);
            }
        }

        public async Task<bool> ShowOAuthRequiredPromptAsync()
        {
            ThrowIfUserInteractionDisabled();

            // Shell out to the UI helper and show the Bitbucket prompt
            if (Context.SessionManager.IsDesktopSession && TryFindHelperExecutablePath(out string helperPath))
            {
                IDictionary<string, string> output = await InvokeHelperAsync(helperPath, "oauth");

                if (output.TryGetValue("continue", out string continueStr) && continueStr.IsTruthy())
                {
                    return true;
                }

                return false;
            }
            else
            {
                ThrowIfTerminalPromptsDisabled();

                Context.Terminal.WriteLine($"Your account has two-factor authentication enabled.{Environment.NewLine}" +
                                           $"To continue you must complete authentication in your web browser.{Environment.NewLine}");

                var _ = Context.Terminal.Prompt("Press enter to continue...");
                return true;
            }
        }

        public async Task<OAuth2TokenResult> CreateOAuthCredentialsAsync(Uri targetUri)
        {
            var oauthClient = new BitbucketOAuth2Client(HttpClient, Context.Settings);

            var browserOptions = new OAuth2WebBrowserOptions
            {
                SuccessResponseHtml = BitbucketResources.AuthenticationResponseSuccessHtml,
                FailureResponseHtmlFormat = BitbucketResources.AuthenticationResponseFailureHtmlFormat
            };

            var browser = new OAuth2SystemWebBrowser(Context.Environment, browserOptions);
            var authCodeResult = await oauthClient.GetAuthorizationCodeAsync(Scopes, browser, CancellationToken.None);

            return await oauthClient.GetTokenByAuthorizationCodeAsync(authCodeResult, CancellationToken.None);
        }

        public async Task<OAuth2TokenResult> RefreshOAuthCredentialsAsync(string refreshToken)
        {
            var oauthClient = new BitbucketOAuth2Client(HttpClient, Context.Settings);

            return await oauthClient.GetTokenByRefreshTokenAsync(refreshToken, CancellationToken.None);
        }

        #endregion

        #region Private Methods

        private bool TryFindHelperExecutablePath(out string path)
        {
            return TryFindHelperExecutablePath(
                BitbucketConstants.EnvironmentVariables.AuthenticationHelper,
                BitbucketConstants.GitConfiguration.Credential.AuthenticationHelper,
                BitbucketConstants.DefaultAuthenticationHelper,
                out path);
        }

        private HttpClient _httpClient;
        private HttpClient HttpClient => _httpClient ??= Context.HttpClientFactory.CreateClient();

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        #endregion
    }
}
