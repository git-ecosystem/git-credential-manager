// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.Authentication;
using Microsoft.Git.CredentialManager.Authentication.OAuth;

namespace GitHub
{
    public interface IGitHubAuthentication : IDisposable
    {
        Task<AuthenticationPromptResult> GetAuthenticationAsync(Uri targetUri, string userName, AuthenticationModes modes);

        Task<string> GetTwoFactorCodeAsync(Uri targetUri, bool isSms);

        Task<OAuth2TokenResult> GetOAuthTokenAsync(Uri targetUri, IEnumerable<string> scopes);
    }

    public class AuthenticationPromptResult
    {
        public AuthenticationPromptResult(AuthenticationModes mode)
        {
            AuthenticationMode = mode;
        }

        public AuthenticationPromptResult(ICredential basicCredential)
            : this(AuthenticationModes.Basic)
        {
            BasicCredential = basicCredential;
        }

        public AuthenticationModes AuthenticationMode { get; }

        public ICredential BasicCredential { get; set; }
    }

    [Flags]
    public enum AuthenticationModes
    {
        None  = 0,
        Basic = 1,
        OAuth = 1 << 1,
    }

    public class GitHubAuthentication : AuthenticationBase, IGitHubAuthentication
    {
        public static readonly string[] AuthorityIds =
        {
            "github",
        };

        public GitHubAuthentication(ICommandContext context)
            : base(context) {}

        public async Task<AuthenticationPromptResult> GetAuthenticationAsync(Uri targetUri, string userName, AuthenticationModes modes)
        {
            ThrowIfUserInteractionDisabled();

            if (modes == AuthenticationModes.None)
            {
                throw new ArgumentException($"Must specify at least one {nameof(AuthenticationModes)}", nameof(modes));
            }

            if (TryFindHelperExecutablePath(out string helperPath))
            {
                var promptArgs = new StringBuilder("prompt");
                if ((modes & AuthenticationModes.Basic) != 0) promptArgs.Append(" --basic");
                if ((modes & AuthenticationModes.OAuth) != 0) promptArgs.Append(" --oauth");
                if (!GitHubHostProvider.IsGitHubDotCom(targetUri)) promptArgs.AppendFormat(" --enterprise-url {0}", targetUri);
                if (!string.IsNullOrWhiteSpace(userName)) promptArgs.AppendFormat("--username {0}", userName);

                IDictionary<string, string> resultDict = await InvokeHelperAsync(helperPath, promptArgs.ToString(), null);

                if (!resultDict.TryGetValue("mode", out string responseMode))
                {
                    throw new Exception("Missing 'mode' in response");
                }

                switch (responseMode.ToLowerInvariant())
                {
                    case "oauth":
                        return new AuthenticationPromptResult(AuthenticationModes.OAuth);

                    case "basic":
                        if (!resultDict.TryGetValue("username", out userName))
                        {
                            throw new Exception("Missing 'username' in response");
                        }

                        if (!resultDict.TryGetValue("password", out string password))
                        {
                            throw new Exception("Missing 'password' in response");
                        }

                        return new AuthenticationPromptResult(new GitCredential(userName, password));

                    default:
                        throw new Exception($"Unknown mode value in response '{responseMode}'");
                }
            }
            else
            {
                ThrowIfTerminalPromptsDisabled();

                switch (modes)
                {
                    case AuthenticationModes.Basic | AuthenticationModes.OAuth:
                        var menuTitle = $"Select an authentication method for '{targetUri}'";
                        var menu = new TerminalMenu(Context.Terminal, menuTitle)
                        {
                            new TerminalMenuItem(1, "Web browser", isDefault: true),
                            new TerminalMenuItem(2, "Username/password")
                        };

                        int option = menu.Show();

                        if (option == 1) goto case AuthenticationModes.OAuth;
                        if (option == 2) goto case AuthenticationModes.Basic;

                        throw new Exception();

                    case AuthenticationModes.Basic:
                        Context.Terminal.WriteLine("Enter GitHub credentials for '{0}'...", targetUri);

                        if (string.IsNullOrWhiteSpace(userName))
                        {
                            userName = Context.Terminal.Prompt("Username");
                        }
                        else
                        {
                            Context.Terminal.WriteLine("Username: {0}", userName);
                        }

                        string password = Context.Terminal.PromptSecret("Password");

                        return new AuthenticationPromptResult(new GitCredential(userName, password));

                    case AuthenticationModes.OAuth:
                        return new AuthenticationPromptResult(AuthenticationModes.OAuth);

                    default:
                        throw new ArgumentOutOfRangeException(nameof(modes), $"Unknown {nameof(AuthenticationModes)} value");
                }
            }
        }

        public async Task<string> GetTwoFactorCodeAsync(Uri targetUri, bool isSms)
        {
            ThrowIfUserInteractionDisabled();

            if (TryFindHelperExecutablePath(out string helperPath))
            {
                IDictionary<string, string> resultDict = await InvokeHelperAsync(helperPath, "2fa", null);

                if (!resultDict.TryGetValue("code", out string authCode))
                {
                    throw new Exception("Missing 'code' in response");
                }

                return authCode;
            }
            else
            {
                ThrowIfTerminalPromptsDisabled();

                Context.Terminal.WriteLine("Two-factor authentication is enabled and an authentication code is required.");

                if (isSms)
                {
                    Context.Terminal.WriteLine("An SMS containing the authentication code has been sent to your registered device.");
                }
                else
                {
                    Context.Terminal.WriteLine("Use your registered authentication app to generate an authentication code.");
                }

                return Context.Terminal.Prompt("Authentication code");
            }
        }

        public async Task<OAuth2TokenResult> GetOAuthTokenAsync(Uri targetUri, IEnumerable<string> scopes)
        {
            ThrowIfUserInteractionDisabled();

            var oauthClient = new GitHubOAuth2Client(HttpClient, Context.Settings, targetUri);

            // If we have a desktop session try authentication using the user's default web browser
            if (Context.SessionManager.IsDesktopSession)
            {
                var browserOptions = new OAuth2WebBrowserOptions
                {
                    SuccessResponseHtml = GitHubResources.AuthenticationResponseSuccessHtml,
                    FailureResponseHtmlFormat = GitHubResources.AuthenticationResponseFailureHtmlFormat
                };
                var browser = new OAuth2SystemWebBrowser(Context.Environment, browserOptions);

                // Write message to the terminal (if any is attached) for some feedback that we're waiting for a web response
                Context.Terminal.WriteLine("info: please complete authentication in your browser...");

                OAuth2AuthorizationCodeResult authCodeResult = await oauthClient.GetAuthorizationCodeAsync(scopes, browser, CancellationToken.None);

                return await oauthClient.GetTokenByAuthorizationCodeAsync(authCodeResult, CancellationToken.None);
            }
            else
            {
                ThrowIfTerminalPromptsDisabled();

                OAuth2DeviceCodeResult deviceCodeResult = await oauthClient.GetDeviceCodeAsync(scopes, CancellationToken.None);

                string deviceMessage = $"To complete authentication please visit {deviceCodeResult.VerificationUri} and enter the following code:" +
                                       Environment.NewLine +
                                       deviceCodeResult.UserCode;
                Context.Terminal.WriteLine(deviceMessage);

                return await oauthClient.GetTokenByDeviceCodeAsync(deviceCodeResult, CancellationToken.None);
            }
        }

        private bool TryFindHelperExecutablePath(out string path)
        {
            return TryFindHelperExecutablePath(
                GitHubConstants.EnvironmentVariables.AuthenticationHelper,
                GitHubConstants.GitConfiguration.Credential.AuthenticationHelper,
                GitHubConstants.DefaultAuthenticationHelper,
                out path);
        }

        private HttpClient _httpClient;
        private HttpClient HttpClient => _httpClient ?? (_httpClient = Context.HttpClientFactory.CreateClient());

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
