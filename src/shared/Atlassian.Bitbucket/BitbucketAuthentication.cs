using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager;
using GitCredentialManager.Authentication;
using GitCredentialManager.Authentication.OAuth;

namespace Atlassian.Bitbucket
{

    [Flags]
    public enum AuthenticationModes
    {
        None = 0,
        Basic = 1,
        OAuth = 1 << 1,

        All = Basic | OAuth
    }
    public interface IBitbucketAuthentication : IDisposable
    {
        Task<CredentialsPromptResult> GetCredentialsAsync(Uri targetUri, string userName, AuthenticationModes modes);
        Task<bool> ShowOAuthRequiredPromptAsync();
        Task<OAuth2TokenResult> CreateOAuthCredentialsAsync(Uri targetUri);
        Task<OAuth2TokenResult> RefreshOAuthCredentialsAsync(string refreshToken);
    }

    public class CredentialsPromptResult
    {
        public CredentialsPromptResult(AuthenticationModes mode)
        {
            AuthenticationMode = mode;
        }

        public CredentialsPromptResult(AuthenticationModes mode, ICredential credential)
            : this(mode)
        {
            Credential = credential;
        }

        public AuthenticationModes AuthenticationMode { get; }

        public ICredential Credential { get; set; }
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

        public async Task<CredentialsPromptResult> GetCredentialsAsync(Uri targetUri, string userName, AuthenticationModes modes)
        {
            ThrowIfUserInteractionDisabled();

            string password;

            // If we don't have a desktop session/GUI then we cannot offer OAuth since the only
            // supported grant is authcode (i.e, using a web browser; device code is not supported).
            if (!Context.SessionManager.IsDesktopSession)
            {
                modes = modes & ~AuthenticationModes.OAuth;
            }

            // If the only supported mode is OAuth then just return immediately
            if (modes == AuthenticationModes.OAuth)
            {
                return new CredentialsPromptResult(AuthenticationModes.OAuth);
            }

            // We need at least one mode!
            if (modes == AuthenticationModes.None)
            {
                throw new ArgumentException(@$"Must specify at least one {nameof(AuthenticationModes)}", nameof(modes));
            }

            // Shell out to the UI helper and show the Bitbucket u/p prompt
            if (Context.Settings.IsGuiPromptsEnabled && Context.SessionManager.IsDesktopSession &&
                TryFindHelperExecutablePath(out string helperPath))
            {
                var cmdArgs = new StringBuilder("userpass");
                if (!string.IsNullOrWhiteSpace(userName))
                {
                    cmdArgs.AppendFormat(" --username {0}", QuoteCmdArg(userName));
                }

                if ((modes & AuthenticationModes.OAuth) != 0)
                {
                    cmdArgs.Append(" --show-oauth");
                }

                IDictionary<string, string> output = await InvokeHelperAsync(helperPath, cmdArgs.ToString());

                if (output.TryGetValue("mode", out string mode) &&
                    StringComparer.OrdinalIgnoreCase.Equals(mode, "oauth"))
                {
                    return new CredentialsPromptResult(AuthenticationModes.OAuth);
                }
                else
                {
                    if (!output.TryGetValue("username", out userName))
                    {
                        throw new Exception("Missing username in response");
                    }

                    if (!output.TryGetValue("password", out password))
                    {
                        throw new Exception("Missing password in response");
                    }

                    return new CredentialsPromptResult(
                        AuthenticationModes.Basic,
                        new GitCredential(userName, password));
                }
            }
            else
            {
                ThrowIfTerminalPromptsDisabled();

                switch (modes)
                {
                    case AuthenticationModes.Basic:
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

                        return new CredentialsPromptResult(
                            AuthenticationModes.Basic,
                            new GitCredential(userName, password));

                    case AuthenticationModes.OAuth:
                        return new CredentialsPromptResult(AuthenticationModes.OAuth);

                    case AuthenticationModes.None:
                        throw new ArgumentOutOfRangeException(nameof(modes), @$"At least one {nameof(AuthenticationModes)} must be supplied");

                    default:
                        var menuTitle = $"Select an authentication method for '{targetUri}'";
                        var menu = new TerminalMenu(Context.Terminal, menuTitle);

                        TerminalMenuItem oauthItem = null;
                        TerminalMenuItem basicItem = null;

                        if ((modes & AuthenticationModes.OAuth) != 0) oauthItem = menu.Add("OAuth");
                        if ((modes & AuthenticationModes.Basic) != 0) basicItem = menu.Add("Username/password");

                        // Default to the 'first' choice in the menu
                        TerminalMenuItem choice = menu.Show(0);

                        if (choice == oauthItem) goto case AuthenticationModes.OAuth;
                        if (choice == basicItem) goto case AuthenticationModes.Basic;

                        throw new Exception();
                }
            }
        }

        public async Task<bool> ShowOAuthRequiredPromptAsync()
        {
            ThrowIfUserInteractionDisabled();

            // Shell out to the UI helper and show the Bitbucket prompt
            if (Context.Settings.IsGuiPromptsEnabled && Context.SessionManager.IsDesktopSession &&
                TryFindHelperExecutablePath(out string helperPath))
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
            ThrowIfUserInteractionDisabled();

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
