using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Atlassian.Bitbucket.UI.ViewModels;
using Atlassian.Bitbucket.UI.Views;
using GitCredentialManager;
using GitCredentialManager.Authentication;
using GitCredentialManager.Authentication.OAuth;
using GitCredentialManager.UI;

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
        Task<OAuth2TokenResult> CreateOAuthCredentialsAsync(InputArguments input);
        Task<OAuth2TokenResult> RefreshOAuthCredentialsAsync(InputArguments input, string refreshToken);
        string GetRefreshTokenServiceName(InputArguments input);
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
            BitbucketConstants.Id,
        };

        private readonly IRegistry<BitbucketOAuth2Client> _oauth2ClientRegistry;

        public BitbucketAuthentication(ICommandContext context)
            : this(context, new OAuth2ClientRegistry(context)) { }

        public BitbucketAuthentication(ICommandContext context, IRegistry<BitbucketOAuth2Client> oauth2ClientRegistry)
    : base(context)
        {
            EnsureArgument.NotNull(oauth2ClientRegistry, nameof(oauth2ClientRegistry));
            this._oauth2ClientRegistry = oauth2ClientRegistry;
        }

        public async Task<CredentialsPromptResult> GetCredentialsAsync(Uri targetUri, string userName, AuthenticationModes modes)
        {
            ThrowIfUserInteractionDisabled();

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
            if (Context.Settings.IsGuiPromptsEnabled && Context.SessionManager.IsDesktopSession)
            {
                if (TryFindHelperCommand(out string helperCommand, out string args))
                {
                    return await GetCredentialsViaHelperAsync(targetUri, userName, modes, helperCommand, args);
                }

                return await GetCredentialsViaUiAsync(targetUri, userName, modes);
            }

            return GetCredentialsViaTty(targetUri, userName, modes);
        }

        private async Task<CredentialsPromptResult> GetCredentialsViaUiAsync(
            Uri targetUri, string userName, AuthenticationModes modes)
        {
            var viewModel = new CredentialsViewModel(Context.Environment)
            {
                ShowOAuth = (modes & AuthenticationModes.OAuth) != 0,
                ShowBasic = (modes & AuthenticationModes.Basic) != 0
            };

            if (!BitbucketHelper.IsBitbucketOrg(targetUri))
            {
                viewModel.Url = targetUri;
            }

            if (!string.IsNullOrWhiteSpace(userName))
            {
                viewModel.UserName = userName;
            }

            await AvaloniaUi.ShowViewAsync<CredentialsView>(viewModel, GetParentWindowHandle(), CancellationToken.None);

            ThrowIfWindowCancelled(viewModel);

            switch (viewModel.SelectedMode)
            {
                case AuthenticationModes.OAuth:
                    return new CredentialsPromptResult(AuthenticationModes.OAuth);

                case AuthenticationModes.Basic:
                    return new CredentialsPromptResult(
                        AuthenticationModes.Basic,
                        new GitCredential(viewModel.UserName, viewModel.Password)
                        );

                default:
                    throw new ArgumentOutOfRangeException(nameof(AuthenticationModes),
                        "Unknown authentication mode", viewModel.SelectedMode.ToString());
            }
        }

        private CredentialsPromptResult GetCredentialsViaTty(Uri targetUri, string userName, AuthenticationModes modes)
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
                    string password = Context.Terminal.PromptSecret("Password");

                    return new CredentialsPromptResult(
                        AuthenticationModes.Basic,
                        new GitCredential(userName, password));

                case AuthenticationModes.OAuth:
                    return new CredentialsPromptResult(AuthenticationModes.OAuth);

                case AuthenticationModes.None:
                    throw new ArgumentOutOfRangeException(nameof(modes),
                        @$"At least one {nameof(AuthenticationModes)} must be supplied");

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

        private async Task<CredentialsPromptResult> GetCredentialsViaHelperAsync(
            Uri targetUri, string userName, AuthenticationModes modes, string helperCommand, string args)
        {
            var promptArgs = new StringBuilder(args);
            promptArgs.Append("prompt");
            if (!BitbucketHelper.IsBitbucketOrg(targetUri))
            {
                promptArgs.AppendFormat(" --url {0}", QuoteCmdArg(targetUri.ToString()));
            }

            if (!string.IsNullOrWhiteSpace(userName))
            {
                promptArgs.AppendFormat(" --username {0}", QuoteCmdArg(userName));
            }

            if ((modes & AuthenticationModes.Basic) != 0)
            {
                promptArgs.Append(" --show-basic");
            }

            if ((modes & AuthenticationModes.OAuth) != 0)
            {
                promptArgs.Append(" --show-oauth");
            }

            IDictionary<string, string> output = await InvokeHelperAsync(helperCommand, promptArgs.ToString());

            if (output.TryGetValue("mode", out string mode) &&
                StringComparer.OrdinalIgnoreCase.Equals(mode, "oauth"))
            {
                return new CredentialsPromptResult(AuthenticationModes.OAuth);
            }
            else
            {
                if (!output.TryGetValue("username", out userName))
                {
                    throw new Trace2Exception(Context.Trace2, "Missing username in response");
                }

                if (!output.TryGetValue("password", out string password))
                {
                    throw new Trace2Exception(Context.Trace2, "Missing password in response");
                }

                return new CredentialsPromptResult(
                    AuthenticationModes.Basic,
                    new GitCredential(userName, password));
            }
        }

        public async Task<OAuth2TokenResult> CreateOAuthCredentialsAsync(InputArguments input)
        {
            ThrowIfUserInteractionDisabled();

            var browserOptions = new OAuth2WebBrowserOptions
            {
                SuccessResponseHtml = BitbucketResources.AuthenticationResponseSuccessHtml,
                FailureResponseHtmlFormat = BitbucketResources.AuthenticationResponseFailureHtmlFormat
            };

            var browser = new OAuth2SystemWebBrowser(Context.Environment, browserOptions);
            var oauth2Client = _oauth2ClientRegistry.Get(input);

            var authCodeResult = await oauth2Client.GetAuthorizationCodeAsync(browser, CancellationToken.None);
            return await oauth2Client.GetTokenByAuthorizationCodeAsync(authCodeResult, CancellationToken.None);
        }

        public async Task<OAuth2TokenResult> RefreshOAuthCredentialsAsync(InputArguments input, string refreshToken)
        {
            var client = _oauth2ClientRegistry.Get(input);
            return await client.GetTokenByRefreshTokenAsync(refreshToken, CancellationToken.None);
        }

        public string GetRefreshTokenServiceName(InputArguments input)
        {
            var client = _oauth2ClientRegistry.Get(input);
            return client.GetRefreshTokenServiceName(input);
        }

        protected internal virtual bool TryFindHelperCommand(out string command, out string args)
        {
            return TryFindHelperCommand(
                BitbucketConstants.EnvironmentVariables.AuthenticationHelper,
                BitbucketConstants.GitConfiguration.Credential.AuthenticationHelper,
                BitbucketConstants.DefaultAuthenticationHelper,
                out command,
                out args);
        }

        private HttpClient _httpClient;
        private HttpClient HttpClient => _httpClient ??= Context.HttpClientFactory.CreateClient();

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
