using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using GitCredentialManager;
using GitCredentialManager.Authentication;
using GitCredentialManager.Authentication.OAuth;
using GitCredentialManager.UI;
using GitLab.UI.ViewModels;
using GitLab.UI.Views;

namespace GitLab
{
    public interface IGitLabAuthentication : IDisposable
    {
        Task<AuthenticationPromptResult> GetAuthenticationAsync(Uri targetUri, string userName, AuthenticationModes modes);

        Task<OAuth2TokenResult> GetOAuthTokenViaBrowserAsync(Uri targetUri, IEnumerable<string> scopes);

        Task<OAuth2TokenResult> GetOAuthTokenViaRefresh(Uri targetUri, string refreshToken);
    }

    public class AuthenticationPromptResult
    {
        public AuthenticationPromptResult(AuthenticationModes mode)
        {
            AuthenticationMode = mode;
        }

        public AuthenticationPromptResult(AuthenticationModes mode, ICredential credential)
            : this(mode)
        {
            Credential = credential;
        }

        public AuthenticationModes AuthenticationMode { get; }

        public ICredential Credential { get; set; }
    }

    [Flags]
    public enum AuthenticationModes
    {
        None = 0,
        Basic = 1,
        Browser = 1 << 1,
        Pat = 1 << 2,

        All = Basic | Browser | Pat
    }

    public class GitLabAuthentication : AuthenticationBase, IGitLabAuthentication
    {
        public GitLabAuthentication(ICommandContext context)
            : base(context) { }

        public async Task<AuthenticationPromptResult> GetAuthenticationAsync(Uri targetUri, string userName, AuthenticationModes modes)
        {
            // If we cannot start a browser then don't offer the option
            if (!Context.SessionManager.IsWebBrowserAvailable)
            {
                modes = modes & ~AuthenticationModes.Browser;
            }

            // We need at least one mode!
            if (modes == AuthenticationModes.None)
            {
                throw new ArgumentException(@$"Must specify at least one {nameof(AuthenticationModes)}", nameof(modes));
            }

            ThrowIfUserInteractionDisabled();

            if (Context.Settings.IsGuiPromptsEnabled && Context.SessionManager.IsDesktopSession)
            {
                if (TryFindHelperCommand(out string helperCommand, out string args))
                {
                    return await GetAuthenticationViaHelperAsync(targetUri, userName, modes, helperCommand, args);
                }

                return await GetAuthenticationViaUiAsync(targetUri, userName, modes);
            }

            return GetAuthenticationViaTty(targetUri, userName, modes);
        }

        private async Task<AuthenticationPromptResult> GetAuthenticationViaUiAsync(
            Uri targetUri, string userName, AuthenticationModes modes)
        {
            var viewModel = new CredentialsViewModel(Context.Environment)
            {
                ShowBrowserLogin = (modes & AuthenticationModes.Browser) != 0,
                ShowTokenLogin   = (modes & AuthenticationModes.Pat) != 0,
                ShowBasicLogin   = (modes & AuthenticationModes.Basic) != 0,
            };

            if (!GitLabConstants.IsGitLabDotCom(targetUri))
            {
                viewModel.Url = targetUri.ToString();
            }

            if (!string.IsNullOrWhiteSpace(userName))
            {
                viewModel.UserName = userName;
                viewModel.TokenUserName = userName;
            }

            await AvaloniaUi.ShowViewAsync<CredentialsView>(viewModel, GetParentWindowHandle(), CancellationToken.None);

            ThrowIfWindowCancelled(viewModel);

            switch (viewModel.SelectedMode)
            {
                case AuthenticationModes.Basic:
                    return new AuthenticationPromptResult(
                        AuthenticationModes.Basic,
                        new GitCredential(viewModel.UserName, viewModel.Password)
                    );

                case AuthenticationModes.Browser:
                    return new AuthenticationPromptResult(AuthenticationModes.Browser);

                case AuthenticationModes.Pat:
                    return new AuthenticationPromptResult(
                        AuthenticationModes.Pat,
                        new GitCredential(viewModel.TokenUserName, viewModel.Token)
                    );

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private AuthenticationPromptResult GetAuthenticationViaTty(Uri targetUri, string userName, AuthenticationModes modes)
        {
            ThrowIfTerminalPromptsDisabled();

            switch (modes)
            {
                case AuthenticationModes.Basic:
                    Context.Terminal.WriteLine("Enter GitLab credentials for '{0}'...", targetUri);

                    if (string.IsNullOrWhiteSpace(userName))
                    {
                        userName = Context.Terminal.Prompt("Username");
                    }
                    else
                    {
                        Context.Terminal.WriteLine("Username: {0}", userName);
                    }

                    string password = Context.Terminal.PromptSecret("Password");
                    return new AuthenticationPromptResult(AuthenticationModes.Basic, new GitCredential(userName, password));

                case AuthenticationModes.Pat:
                    Context.Terminal.WriteLine("Enter GitLab credentials for '{0}'...", targetUri);

                    if (string.IsNullOrWhiteSpace(userName))
                    {
                        userName = Context.Terminal.Prompt("Username");
                    }
                    else
                    {
                        Context.Terminal.WriteLine("Username: {0}", userName);
                    }

                    string token = Context.Terminal.PromptSecret("Personal access token");
                    return new AuthenticationPromptResult(AuthenticationModes.Pat, new GitCredential(userName, token));

                case AuthenticationModes.Browser:
                    return new AuthenticationPromptResult(AuthenticationModes.Browser);

                case AuthenticationModes.None:
                    throw new ArgumentOutOfRangeException(nameof(modes),
                        @$"At least one {nameof(AuthenticationModes)} must be supplied");

                default:
                    var menuTitle = $"Select an authentication method for '{targetUri}'";
                    var menu = new TerminalMenu(Context.Terminal, menuTitle);

                    TerminalMenuItem browserItem = null;
                    TerminalMenuItem basicItem = null;
                    TerminalMenuItem patItem = null;

                    if ((modes & AuthenticationModes.Browser) != 0) browserItem = menu.Add("Web browser");
                    if ((modes & AuthenticationModes.Pat) != 0) patItem = menu.Add("Personal access token");
                    if ((modes & AuthenticationModes.Basic) != 0) basicItem = menu.Add("Username/password");

                    // Default to the 'first' choice in the menu
                    TerminalMenuItem choice = menu.Show(0);

                    if (choice == browserItem) goto case AuthenticationModes.Browser;
                    if (choice == basicItem) goto case AuthenticationModes.Basic;
                    if (choice == patItem) goto case AuthenticationModes.Pat;

                    throw new Exception();
            }
        }

        private async Task<AuthenticationPromptResult> GetAuthenticationViaHelperAsync(
            Uri targetUri, string userName, AuthenticationModes modes, string helperCommand, string args)
        {
            var promptArgs = new StringBuilder(args);
            promptArgs.Append("prompt");
            if (!string.IsNullOrWhiteSpace(userName))
            {
                promptArgs.AppendFormat(" --username {0}", QuoteCmdArg(userName));
            }

            promptArgs.AppendFormat(" --url {0}", QuoteCmdArg(targetUri.ToString()));

            if ((modes & AuthenticationModes.Basic) != 0) promptArgs.Append(" --basic");
            if ((modes & AuthenticationModes.Browser) != 0) promptArgs.Append(" --browser");
            if ((modes & AuthenticationModes.Pat) != 0) promptArgs.Append(" --pat");

            IDictionary<string, string> resultDict = await InvokeHelperAsync(helperCommand, promptArgs.ToString());

            if (!resultDict.TryGetValue("mode", out string responseMode))
            {
                throw new Trace2Exception(Context.Trace2, "Missing 'mode' in response");
            }

            switch (responseMode.ToLowerInvariant())
            {
                case "pat":
                    if (!resultDict.TryGetValue("pat", out string pat))
                    {
                        throw new Trace2Exception(Context.Trace2, "Missing 'pat' in response");
                    }

                    if (!resultDict.TryGetValue("username", out string patUserName))
                    {
                        // Username is optional for PATs
                    }

                    return new AuthenticationPromptResult(
                        AuthenticationModes.Pat, new GitCredential(patUserName, pat));

                case "browser":
                    return new AuthenticationPromptResult(AuthenticationModes.Browser);

                case "basic":
                    if (!resultDict.TryGetValue("username", out userName))
                    {
                        throw new Trace2Exception(Context.Trace2, "Missing 'username' in response");
                    }

                    if (!resultDict.TryGetValue("password", out string password))
                    {
                        throw new Trace2Exception(Context.Trace2, "Missing 'password' in response");
                    }

                    return new AuthenticationPromptResult(
                        AuthenticationModes.Basic, new GitCredential(userName, password));

                default:
                    throw new Trace2Exception(Context.Trace2,
                        $"Unknown mode value in response '{responseMode}'");
            }
        }

        public async Task<OAuth2TokenResult> GetOAuthTokenViaBrowserAsync(Uri targetUri, IEnumerable<string> scopes)
        {
            ThrowIfUserInteractionDisabled();

            var oauthClient = new GitLabOAuth2Client(HttpClient, Context.Settings, targetUri, Context.Trace2);

            // We require a desktop session to launch the user's default web browser
            if (!Context.SessionManager.IsDesktopSession)
            {
                throw new Trace2InvalidOperationException(Context.Trace2,
                    "Browser authentication requires a desktop session");
            }

            var browserOptions = new OAuth2WebBrowserOptions { };
            var browser = new OAuth2SystemWebBrowser(Context.Environment, browserOptions);

            // Write message to the terminal (if any is attached) for some feedback that we're waiting for a web response
            Context.Terminal.WriteLine("info: please complete authentication in your browser...");

            OAuth2AuthorizationCodeResult authCodeResult =
                await oauthClient.GetAuthorizationCodeAsync(scopes, browser, CancellationToken.None);

            return await oauthClient.GetTokenByAuthorizationCodeAsync(authCodeResult, CancellationToken.None);
        }

        public async Task<OAuth2TokenResult> GetOAuthTokenViaRefresh(Uri targetUri, string refreshToken)
        {
            var oauthClient = new GitLabOAuth2Client(HttpClient, Context.Settings, targetUri, Context.Trace2);
            return await oauthClient.GetTokenByRefreshTokenAsync(refreshToken, CancellationToken.None);
        }

        private bool TryFindHelperCommand(out string command, out string args)
        {
            return TryFindHelperCommand(
                GitLabConstants.EnvironmentVariables.AuthenticationHelper,
                GitLabConstants.GitConfiguration.Credential.AuthenticationHelper,
                GitLabConstants.DefaultAuthenticationHelper,
                out command,
                out args);
        }

        private HttpClient _httpClient;
        private HttpClient HttpClient => _httpClient ?? (_httpClient = Context.HttpClientFactory.CreateClient());

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
