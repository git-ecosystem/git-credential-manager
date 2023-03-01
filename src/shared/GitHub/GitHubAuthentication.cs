using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using GitCredentialManager;
using GitCredentialManager.Authentication;
using GitCredentialManager.Authentication.OAuth;
using GitCredentialManager.UI;
using GitHub.UI.ViewModels;
using GitHub.UI.Views;

namespace GitHub
{
    public interface IGitHubAuthentication : IDisposable
    {
        Task<string> SelectAccountAsync(Uri targetUri, IEnumerable<string> accounts);

        Task<AuthenticationPromptResult> GetAuthenticationAsync(Uri targetUri, string userName, AuthenticationModes modes);

        Task<string> GetTwoFactorCodeAsync(Uri targetUri, bool isSms);

        Task<OAuth2TokenResult> GetOAuthTokenViaBrowserAsync(Uri targetUri, IEnumerable<string> scopes, string loginHint);

        Task<OAuth2TokenResult> GetOAuthTokenViaDeviceCodeAsync(Uri targetUri, IEnumerable<string> scopes);
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
        None  = 0,
        Basic = 1,
        Browser = 1 << 1,
        Pat     = 1 << 2,
        Device  = 1 << 3,

        OAuth = Browser | Device,
        All   = Basic | OAuth | Pat
    }

    public class GitHubAuthentication : AuthenticationBase, IGitHubAuthentication
    {
        public static readonly string[] AuthorityIds =
        {
            "github",
        };

        public GitHubAuthentication(ICommandContext context)
            : base(context) {}

        public async Task<string> SelectAccountAsync(Uri targetUri, IEnumerable<string> accounts)
        {
            ThrowIfUserInteractionDisabled();

            if (Context.Settings.IsGuiPromptsEnabled && Context.SessionManager.IsDesktopSession)
            {
                if (TryFindHelperCommand(out string command, out string args))
                {
                    var promptArgs = new StringBuilder(args);
                    promptArgs.Append("select-account");

                    if (!GitHubHostProvider.IsGitHubDotCom(targetUri))
                    {
                        promptArgs.AppendFormat(" --enterprise-url {0}", QuoteCmdArg(targetUri.ToString()));
                    }

                    // Write the accounts to the standard input of the helper process to avoid any issues
                    // with escaping special characters, and to avoid max argument length problems.
                    byte[] bytes = Encoding.UTF8.GetBytes(string.Join("\n", accounts));
                    using var ms = new MemoryStream(bytes);
                    using var stdin = new StreamReader(ms);

                    IDictionary<string, string> resultDict = await InvokeHelperAsync(command, promptArgs.ToString(), stdin);

                    if (!resultDict.TryGetValue("account", out string selectedAccount))
                    {
                        throw new Exception("Missing 'account' in response");
                    }

                    return string.IsNullOrWhiteSpace(selectedAccount) ? null : selectedAccount;
                }

                var viewModel = new SelectAccountViewModel(Context.Environment, accounts);

                if (!GitHubHostProvider.IsGitHubDotCom(targetUri))
                {
                    viewModel.EnterpriseUrl = targetUri.ToString();
                }

                await AvaloniaUi.ShowViewAsync<SelectAccountView>(viewModel, GetParentWindowHandle(), CancellationToken.None);

                ThrowIfWindowCancelled(viewModel);

                return viewModel.SelectedAccount?.UserName;
            }

            ThrowIfTerminalPromptsDisabled();
            var menuTitle = $"Select an account for '{targetUri}'";
            var menu = new TerminalMenu(Context.Terminal, menuTitle);
            var addNewItem = menu.Add("Add a new account");

            foreach (string account in accounts)
            {
                menu.Add(account);
            }

            TerminalMenuItem choice = menu.Show();
            return choice == addNewItem ? null : choice.Name;
        }

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

            // If there is no mode choice to be made and no interaction required,
            // just return that result.
            if (modes == AuthenticationModes.Browser ||
                modes == AuthenticationModes.Device)
            {
                return new AuthenticationPromptResult(modes);
            }

            ThrowIfUserInteractionDisabled();

            if (Context.Settings.IsGuiPromptsEnabled && Context.SessionManager.IsDesktopSession)
            {
                if (TryFindHelperCommand(out string command, out string args))
                {
                    return await GetAuthenticationViaHelperAsync(targetUri, userName, modes, command, args);
                }

                return await GetAuthenticationViaUiAsync(targetUri, userName, modes);
            }

            return GetAuthenticationViaTty(targetUri, userName, modes);
        }

        private async Task<AuthenticationPromptResult> GetAuthenticationViaUiAsync(
            Uri targetUri, string userName, AuthenticationModes modes)
        {
            var viewModel = new CredentialsViewModel(Context.Environment, Context.ProcessManager)
            {
                ShowBrowserLogin = (modes & AuthenticationModes.Browser) != 0,
                ShowDeviceLogin  = (modes & AuthenticationModes.Device) != 0,
                ShowTokenLogin   = (modes & AuthenticationModes.Pat) != 0,
                ShowBasicLogin   = (modes & AuthenticationModes.Basic) != 0,
            };

            if (!GitHubHostProvider.IsGitHubDotCom(targetUri))
            {
                viewModel.EnterpriseUrl = targetUri.ToString();
            }

            if (!string.IsNullOrWhiteSpace(userName))
            {
                viewModel.UserName = userName;
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

                case AuthenticationModes.Device:
                    return new AuthenticationPromptResult(AuthenticationModes.Device);

                case AuthenticationModes.Pat:
                    return new AuthenticationPromptResult(
                        AuthenticationModes.Pat,
                        new GitCredential(userName, viewModel.Token)
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

                    return new AuthenticationPromptResult(
                        AuthenticationModes.Basic, new GitCredential(userName, password));

                case AuthenticationModes.Browser:
                    return new AuthenticationPromptResult(AuthenticationModes.Browser);

                case AuthenticationModes.Device:
                    return new AuthenticationPromptResult(AuthenticationModes.Device);

                case AuthenticationModes.Pat:
                    Context.Terminal.WriteLine("Enter GitHub personal access token for '{0}'...", targetUri);
                    string pat = Context.Terminal.PromptSecret("Token");
                    return new AuthenticationPromptResult(
                        AuthenticationModes.Pat, new GitCredential(userName, pat));

                case AuthenticationModes.None:
                    throw new ArgumentOutOfRangeException(nameof(modes),
                        @$"At least one {nameof(AuthenticationModes)} must be supplied");

                default:
                    var menuTitle = $"Select an authentication method for '{targetUri}'";
                    var menu = new TerminalMenu(Context.Terminal, menuTitle);

                    TerminalMenuItem browserItem = null;
                    TerminalMenuItem deviceItem = null;
                    TerminalMenuItem basicItem = null;
                    TerminalMenuItem patItem = null;

                    if ((modes & AuthenticationModes.Browser) != 0) browserItem = menu.Add("Web browser");
                    if ((modes & AuthenticationModes.Device) != 0) deviceItem = menu.Add("Device code");
                    if ((modes & AuthenticationModes.Pat) != 0) patItem = menu.Add("Personal access token");
                    if ((modes & AuthenticationModes.Basic) != 0) basicItem = menu.Add("Username/password");

                    // Default to the 'first' choice in the menu
                    TerminalMenuItem choice = menu.Show(0);

                    if (choice == browserItem) goto case AuthenticationModes.Browser;
                    if (choice == deviceItem) goto case AuthenticationModes.Device;
                    if (choice == basicItem) goto case AuthenticationModes.Basic;
                    if (choice == patItem) goto case AuthenticationModes.Pat;

                    throw new Exception();
            }
        }

        private async Task<AuthenticationPromptResult> GetAuthenticationViaHelperAsync(
            Uri targetUri, string userName, AuthenticationModes modes, string command, string args)
        {
            var promptArgs = new StringBuilder(args);
            promptArgs.Append("prompt");
            if (modes == AuthenticationModes.All)
            {
                promptArgs.Append(" --all");
            }
            else
            {
                if ((modes & AuthenticationModes.Basic) != 0) promptArgs.Append(" --basic");
                if ((modes & AuthenticationModes.Browser) != 0) promptArgs.Append(" --browser");
                if ((modes & AuthenticationModes.Device) != 0) promptArgs.Append(" --device");
                if ((modes & AuthenticationModes.Pat) != 0) promptArgs.Append(" --pat");
            }

            if (!GitHubHostProvider.IsGitHubDotCom(targetUri))
                promptArgs.AppendFormat(" --enterprise-url {0}", QuoteCmdArg(targetUri.ToString()));
            if (!string.IsNullOrWhiteSpace(userName)) promptArgs.AppendFormat(" --username {0}", QuoteCmdArg(userName));

            IDictionary<string, string> resultDict = await InvokeHelperAsync(command, promptArgs.ToString(), null);

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

                    return new AuthenticationPromptResult(
                        AuthenticationModes.Pat, new GitCredential(userName, pat));

                case "browser":
                    return new AuthenticationPromptResult(AuthenticationModes.Browser);

                case "device":
                    return new AuthenticationPromptResult(AuthenticationModes.Device);

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

        public async Task<string> GetTwoFactorCodeAsync(Uri targetUri, bool isSms)
        {
            ThrowIfUserInteractionDisabled();

            if (Context.Settings.IsGuiPromptsEnabled && Context.SessionManager.IsDesktopSession)
            {
                if (TryFindHelperCommand(out string command, out string args))
                {
                    return await GetTwoFactorCodeViaHelperAsync(isSms, args, command);
                }

                return await GetTwoFactorCodeViaUiAsync(targetUri, isSms);
            }

            return GetTwoFactorCodeViaTty(isSms);
        }

        private async Task<string> GetTwoFactorCodeViaUiAsync(Uri targetUri, bool isSms)
        {
            var viewModel = new TwoFactorViewModel(Context.Environment, Context.ProcessManager)
            {
                IsSms = isSms
            };

            await AvaloniaUi.ShowViewAsync<TwoFactorView>(viewModel, GetParentWindowHandle(), CancellationToken.None);
            
            ThrowIfWindowCancelled(viewModel);

            return viewModel.Code;
        }

        private string GetTwoFactorCodeViaTty(bool isSms)
        {
            ThrowIfTerminalPromptsDisabled();

            Context.Terminal.WriteLine("Two-factor authentication is enabled and an authentication code is required.");

            Context.Terminal.WriteLine(isSms
                ? "An SMS containing the authentication code has been sent to your registered device."
                : "Use your registered authentication app to generate an authentication code.");

            return Context.Terminal.Prompt("Authentication code");
        }

        private async Task<string> GetTwoFactorCodeViaHelperAsync(bool isSms, string args, string command)
        {
            var promptArgs = new StringBuilder(args);
            promptArgs.Append("2fa");
            if (isSms) promptArgs.Append(" --sms");

            IDictionary<string, string> resultDict = await InvokeHelperAsync(command, promptArgs.ToString(), null);

            if (!resultDict.TryGetValue("code", out string authCode))
            {
                throw new Trace2Exception(Context.Trace2, "Missing 'code' in response");
            }

            return authCode;
        }

        public async Task<OAuth2TokenResult> GetOAuthTokenViaBrowserAsync(Uri targetUri, IEnumerable<string> scopes, string loginHint)
        {
            ThrowIfUserInteractionDisabled();

            var oauthClient = new GitHubOAuth2Client(HttpClient, Context.Settings, targetUri, Context.Trace2);

            // Can we launch the user's default web browser?
            if (!Context.SessionManager.IsWebBrowserAvailable)
            {
                throw new Trace2InvalidOperationException(Context.Trace2,
                    "Browser authentication requires a desktop session");
            }

            var browserOptions = new OAuth2WebBrowserOptions
            {
                SuccessResponseHtml = GitHubResources.AuthenticationResponseSuccessHtml,
                FailureResponseHtmlFormat = GitHubResources.AuthenticationResponseFailureHtmlFormat
            };
            var browser = new OAuth2SystemWebBrowser(Context.Environment, browserOptions);

            // If we have a login hint we should pass this to GitHub as an extra query parameter
            IDictionary<string, string> queryParams = null;
            if (loginHint != null)
            {
                queryParams = new Dictionary<string, string>
                {
                    ["login"] = loginHint
                };
            }

            // Write message to the terminal (if any is attached) for some feedback that we're waiting for a web response
            Context.Terminal.WriteLine("info: please complete authentication in your browser...");

            OAuth2AuthorizationCodeResult authCodeResult =
                await oauthClient.GetAuthorizationCodeAsync(scopes, browser, queryParams, CancellationToken.None);

            return await oauthClient.GetTokenByAuthorizationCodeAsync(authCodeResult, CancellationToken.None);
        }

        public async Task<OAuth2TokenResult> GetOAuthTokenViaDeviceCodeAsync(Uri targetUri, IEnumerable<string> scopes)
        {
            ThrowIfUserInteractionDisabled();

            var oauthClient = new GitHubOAuth2Client(HttpClient, Context.Settings, targetUri, Context.Trace2);
            OAuth2DeviceCodeResult dcr = await oauthClient.GetDeviceCodeAsync(scopes, CancellationToken.None);

            // If we have a desktop session show the device code in a dialog
            if (Context.Settings.IsGuiPromptsEnabled && Context.SessionManager.IsDesktopSession)
            {
                var promptCts = new CancellationTokenSource();
                var tokenCts = new CancellationTokenSource();

                // Show the dialog with the device code but don't await its closure
                Task promptTask = TryFindHelperCommand(out string command, out string args)
                    ? ShowDeviceCodeViaHelperAsync(dcr, command, args, promptCts.Token)
                    : ShowDeviceCodeViaUiAsync(dcr, promptCts.Token);

                // Start the request for an OAuth token but don't wait
                Task<OAuth2TokenResult> tokenTask = oauthClient.GetTokenByDeviceCodeAsync(dcr, tokenCts.Token);

                Task t = await Task.WhenAny(promptTask, tokenTask);

                // If the dialog was closed the user wishes to cancel the request
                if (t == promptTask)
                {
                    tokenCts.Cancel();
                }

                OAuth2TokenResult tokenResult;
                try
                {
                    tokenResult = await tokenTask;
                }
                catch (OperationCanceledException)
                {
                    throw new Trace2InvalidOperationException(Context.Trace2,
                        "User canceled device code authentication");
                }

                // Close the dialog
                promptCts.Cancel();

                return tokenResult;
            }

            return await GetOAuthTokenViaDeviceCodeViaTtyAsync(oauthClient, dcr);
        }

        private Task ShowDeviceCodeViaUiAsync(OAuth2DeviceCodeResult dcr, CancellationToken ct)
        {
            var viewModel = new DeviceCodeViewModel(Context.Environment)
            {
                UserCode = dcr.UserCode,
                VerificationUrl = dcr.VerificationUri.ToString(),
            };

            return AvaloniaUi.ShowViewAsync<DeviceCodeView>(viewModel, GetParentWindowHandle(), ct);
        }

        private async Task<OAuth2TokenResult> GetOAuthTokenViaDeviceCodeViaTtyAsync(GitHubOAuth2Client oauthClient, OAuth2DeviceCodeResult dcr)
        {
            ThrowIfTerminalPromptsDisabled();

            string deviceMessage =
                $"To complete authentication please visit {dcr.VerificationUri} and enter the following code:" +
                Environment.NewLine +
                dcr.UserCode;
            Context.Terminal.WriteLine(deviceMessage);

            return await oauthClient.GetTokenByDeviceCodeAsync(dcr, CancellationToken.None);
        }

        private Task ShowDeviceCodeViaHelperAsync(
            OAuth2DeviceCodeResult dcr, string command, string args, CancellationToken ct)
        {
            var promptArgs = new StringBuilder(args);
            promptArgs.Append("device");
            promptArgs.AppendFormat(" --code {0} ", QuoteCmdArg(dcr.UserCode));
            promptArgs.AppendFormat(" --url {0}", QuoteCmdArg(dcr.VerificationUri.ToString()));

            return InvokeHelperAsync(command, promptArgs.ToString(), null, ct);
        }

        private bool TryFindHelperCommand(out string command, out string args)
        {
            return TryFindHelperCommand(
                GitHubConstants.EnvironmentVariables.AuthenticationHelper,
                GitHubConstants.GitConfiguration.Credential.AuthenticationHelper,
                GitHubConstants.DefaultAuthenticationHelper,
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
