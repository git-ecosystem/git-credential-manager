using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading;
using GitCredentialManager;
using GitCredentialManager.Authentication;
using GitCredentialManager.Authentication.OAuth;

namespace GitHub
{
    public interface IGitHubAuthentication : IDisposable
    {
        Task<AuthenticationPromptResult> GetAuthenticationAsync(Uri targetUri, string userName, AuthenticationModes modes);

        Task<string> GetTwoFactorCodeAsync(Uri targetUri, bool isSms);

        Task<OAuth2TokenResult> GetOAuthTokenViaBrowserAsync(Uri targetUri, IEnumerable<string> scopes);

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

        public async Task<AuthenticationPromptResult> GetAuthenticationAsync(Uri targetUri, string userName, AuthenticationModes modes)
        {
            ThrowIfUserInteractionDisabled();

            // If we don't have a desktop session/GUI then we cannot offer browser
            if (!Context.SessionManager.IsDesktopSession)
            {
                modes = modes & ~AuthenticationModes.Browser;
            }

            // We need at least one mode!
            if (modes == AuthenticationModes.None)
            {
                throw new ArgumentException(@$"Must specify at least one {nameof(AuthenticationModes)}", nameof(modes));
            }

            if (Context.SessionManager.IsDesktopSession && TryFindHelperExecutablePath(out string helperPath))
            {
                var promptArgs = new StringBuilder("prompt");
                if (modes == AuthenticationModes.All)
                {
                    promptArgs.Append(" --all");
                }
                else
                {
                    if ((modes & AuthenticationModes.Basic)   != 0) promptArgs.Append(" --basic");
                    if ((modes & AuthenticationModes.Browser) != 0) promptArgs.Append(" --browser");
                    if ((modes & AuthenticationModes.Device)  != 0) promptArgs.Append(" --device");
                    if ((modes & AuthenticationModes.Pat)     != 0) promptArgs.Append(" --pat");
                }
                if (!GitHubHostProvider.IsGitHubDotCom(targetUri)) promptArgs.AppendFormat(" --enterprise-url {0}", QuoteCmdArg(targetUri.ToString()));
                if (!string.IsNullOrWhiteSpace(userName)) promptArgs.AppendFormat(" --username {0}", QuoteCmdArg(userName));

                IDictionary<string, string> resultDict = await InvokeHelperAsync(helperPath, promptArgs.ToString(), null);

                if (!resultDict.TryGetValue("mode", out string responseMode))
                {
                    throw new Exception("Missing 'mode' in response");
                }

                switch (responseMode.ToLowerInvariant())
                {
                    case "pat":
                        if (!resultDict.TryGetValue("pat", out string pat))
                        {
                            throw new Exception("Missing 'pat' in response");
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
                            throw new Exception("Missing 'username' in response");
                        }

                        if (!resultDict.TryGetValue("password", out string password))
                        {
                            throw new Exception("Missing 'password' in response");
                        }

                        return new AuthenticationPromptResult(
                            AuthenticationModes.Basic, new GitCredential(userName, password));

                    default:
                        throw new Exception($"Unknown mode value in response '{responseMode}'");
                }
            }
            else
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
                        throw new ArgumentOutOfRangeException(nameof(modes), @$"At least one {nameof(AuthenticationModes)} must be supplied");

                    default:
                        var menuTitle = $"Select an authentication method for '{targetUri}'";
                        var menu = new TerminalMenu(Context.Terminal, menuTitle);

                        TerminalMenuItem browserItem = null;
                        TerminalMenuItem deviceItem = null;
                        TerminalMenuItem basicItem = null;
                        TerminalMenuItem patItem = null;

                        if ((modes & AuthenticationModes.Browser) != 0) browserItem = menu.Add("Web browser");
                        if ((modes & AuthenticationModes.Device)  != 0) deviceItem = menu.Add("Device code");
                        if ((modes & AuthenticationModes.Pat)     != 0) patItem   = menu.Add("Personal access token");
                        if ((modes & AuthenticationModes.Basic)   != 0) basicItem = menu.Add("Username/password");

                        // Default to the 'first' choice in the menu
                        TerminalMenuItem choice = menu.Show(0);

                        if (choice == browserItem) goto case AuthenticationModes.Browser;
                        if (choice == deviceItem)  goto case AuthenticationModes.Device;
                        if (choice == basicItem)   goto case AuthenticationModes.Basic;
                        if (choice == patItem)     goto case AuthenticationModes.Pat;

                        throw new Exception();
                }
            }
        }

        public async Task<string> GetTwoFactorCodeAsync(Uri targetUri, bool isSms)
        {
            ThrowIfUserInteractionDisabled();

            if (Context.SessionManager.IsDesktopSession && TryFindHelperExecutablePath(out string helperPath))
            {
                var args = new StringBuilder("2fa");
                if (isSms) args.Append(" --sms");

                IDictionary<string, string> resultDict = await InvokeHelperAsync(helperPath, args.ToString(), null);

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

        public async Task<OAuth2TokenResult> GetOAuthTokenViaBrowserAsync(Uri targetUri, IEnumerable<string> scopes)
        {
            ThrowIfUserInteractionDisabled();

            var oauthClient = new GitHubOAuth2Client(HttpClient, Context.Settings, targetUri);

            // We require a desktop session to launch the user's default web browser
            if (!Context.SessionManager.IsDesktopSession)
            {
                throw new InvalidOperationException("Browser authentication requires a desktop session");
            }

            var browserOptions = new OAuth2WebBrowserOptions
            {
                SuccessResponseHtml = GitHubResources.AuthenticationResponseSuccessHtml,
                FailureResponseHtmlFormat = GitHubResources.AuthenticationResponseFailureHtmlFormat
            };
            var browser = new OAuth2SystemWebBrowser(Context.Environment, browserOptions);

            // Write message to the terminal (if any is attached) for some feedback that we're waiting for a web response
            Context.Terminal.WriteLine("info: please complete authentication in your browser...");

            OAuth2AuthorizationCodeResult authCodeResult =
                await oauthClient.GetAuthorizationCodeAsync(scopes, browser, CancellationToken.None);

            return await oauthClient.GetTokenByAuthorizationCodeAsync(authCodeResult, CancellationToken.None);
        }

        public async Task<OAuth2TokenResult> GetOAuthTokenViaDeviceCodeAsync(Uri targetUri, IEnumerable<string> scopes)
        {
            ThrowIfUserInteractionDisabled();

            var oauthClient = new GitHubOAuth2Client(HttpClient, Context.Settings, targetUri);
            OAuth2DeviceCodeResult dcr = await oauthClient.GetDeviceCodeAsync(scopes, CancellationToken.None);

            // If we have a desktop session show the device code in a dialog
            if (Context.SessionManager.IsDesktopSession && TryFindHelperExecutablePath(out string helperPath))
            {
                var args = new StringBuilder("device");
                args.AppendFormat(" --code {0} ", QuoteCmdArg(dcr.UserCode));
                args.AppendFormat(" --url {0}", QuoteCmdArg(dcr.VerificationUri.ToString()));

                var promptCts = new CancellationTokenSource();
                var tokenCts = new CancellationTokenSource();

                // Show the dialog with the device code but don't await its closure
                Task promptTask = InvokeHelperAsync(helperPath, args.ToString(), null, promptCts.Token);

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
                    throw new Exception("User canceled device code authentication");
                }

                // Close the dialog
                promptCts.Cancel();

                return tokenResult;
            }
            else
            {
                ThrowIfTerminalPromptsDisabled();

                string deviceMessage = $"To complete authentication please visit {dcr.VerificationUri} and enter the following code:" +
                                       Environment.NewLine +
                                       dcr.UserCode;
                Context.Terminal.WriteLine(deviceMessage);

                return await oauthClient.GetTokenByDeviceCodeAsync(dcr, CancellationToken.None);
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
