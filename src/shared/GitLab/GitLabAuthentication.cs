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

namespace GitLab
{
    public interface IGitLabAuthentication : IDisposable
    {
        AuthenticationPromptResult GetAuthentication(Uri targetUri, string userName, AuthenticationModes modes);

        Task<OAuth2TokenResult> GetOAuthTokenViaBrowserAsync(Uri targetUri, IEnumerable<string> scopes);
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

        public AuthenticationPromptResult GetAuthentication(Uri targetUri, string userName, AuthenticationModes modes)
        {
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

            switch (modes)
            {
                case AuthenticationModes.Basic:
                case AuthenticationModes.Pat:
                    ThrowIfUserInteractionDisabled();
                    ThrowIfTerminalPromptsDisabled();
                    Context.Terminal.WriteLine("Enter GitLab credentials for '{0}'...", targetUri);

                    if (string.IsNullOrWhiteSpace(userName))
                    {
                        userName = Context.Terminal.Prompt("Username");
                    }
                    else
                    {
                        Context.Terminal.WriteLine("Username: {0}", userName);
                    }

                    string password_or_token = Context.Terminal.PromptSecret(modes == AuthenticationModes.Basic ? "Password" : "Personal access token");
                    return new AuthenticationPromptResult(modes, new GitCredential(userName, password_or_token));

                case AuthenticationModes.Browser:
                    return new AuthenticationPromptResult(AuthenticationModes.Browser);

                case AuthenticationModes.None:
                    throw new ArgumentOutOfRangeException(nameof(modes), @$"At least one {nameof(AuthenticationModes)} must be supplied");

                default:
                    ThrowIfUserInteractionDisabled();
                    ThrowIfTerminalPromptsDisabled();
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

        public async Task<OAuth2TokenResult> GetOAuthTokenViaBrowserAsync(Uri targetUri, IEnumerable<string> scopes)
        {
            ThrowIfUserInteractionDisabled();

            var oauthClient = new GitLabOAuth2Client(HttpClient, Context.Settings, targetUri);

            // We require a desktop session to launch the user's default web browser
            if (!Context.SessionManager.IsDesktopSession)
            {
                throw new InvalidOperationException("Browser authentication requires a desktop session");
            }

            var browserOptions = new OAuth2WebBrowserOptions { };
            var browser = new OAuth2SystemWebBrowser(Context.Environment, browserOptions);

            // Write message to the terminal (if any is attached) for some feedback that we're waiting for a web response
            Context.Terminal.WriteLine("info: please complete authentication in your browser...");

            OAuth2AuthorizationCodeResult authCodeResult =
                await oauthClient.GetAuthorizationCodeAsync(scopes, browser, CancellationToken.None);

            return await oauthClient.GetTokenByAuthorizationCodeAsync(authCodeResult, CancellationToken.None);
        }

        private HttpClient _httpClient;
        private HttpClient HttpClient => _httpClient ?? (_httpClient = Context.HttpClientFactory.CreateClient());

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
