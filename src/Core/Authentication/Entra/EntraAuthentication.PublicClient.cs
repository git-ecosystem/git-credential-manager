using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager.Interop.Windows.Native;
using GitCredentialManager.Tty;
using GitCredentialManager.UI;
using GitCredentialManager.UI.Controls;
using GitCredentialManager.UI.ViewModels;
using GitCredentialManager.UI.Views;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using Spectre.Console;

namespace GitCredentialManager.Authentication.Entra
{
    public record PublicClientConfig
    {
        /// <summary>
        /// Application (client) ID of the Entra application registration.
        /// </summary>
        public string ClientId { get; init; }

        /// <summary>
        /// Use the shared Microsoft Developer Tools identity cache.
        /// </summary>
        /// <remarks>
        /// If <see langword="true"/> then use the token cache shared by Microsoft
        /// developer tools such as the Azure PowerShell CLI. Otherwise, use the
        /// Git Credential Manager cache, used only by GCM.
        /// </remarks>
        public bool UseSharedCache { get; init; }
    }

    public enum MicrosoftAuthenticationFlowType
    {
        Auto = 0,
        EmbeddedWebView = 1,
        SystemWebView = 2,
        DeviceCode = 3
    }

    public partial class EntraAuthentication
    {
        private readonly PublicClientConfig _publicClientConfig;
        private PublicClientApplicationBuilder _publicBuilder;

        public async Task<IReadOnlyList<IEntraAccount>> GetUserAccountsAsync(CancellationToken ct = default)
        {
            IPublicClientApplication app = GetPublicAppBuilder().Build();
            await RegisterCacheAsync(app);

            IEnumerable<IAccount> accounts = await app.GetAccountsAsync();
            return accounts.Select(EntraAccount.FromMsalAccount).ToList().AsReadOnly();
        }

        public async Task<bool> RemoveUserAccountAsync(IEntraAccount account)
        {
            IPublicClientApplication app = GetPublicAppBuilder().Build();
            await RegisterCacheAsync(app);

            IAccount msalAccount = await ResolveAccountAsync(app, account);
            if (msalAccount is null)
            {
                return false;
            }

            Context.Trace.WriteLine(
                $"Removing account '{msalAccount.HomeAccountId.Identifier}' ({msalAccount.Username}) from the cache...");
            await app.RemoveAsync(msalAccount);
            return true;
        }

        private async Task<IAccount> ResolveAccountAsync(IPublicClientApplication app, IEntraAccount account)
        {
            // If we have been handed a wrapped MSAL account there is no need to search the cache again
            if (account is EntraAccount { MsalAccount: not null } wrapped)
            {
                Context.Trace.WriteLine($"Account '{account.HomeAccountId}' ({account.UserName}) is already from cache.");
                return wrapped.MsalAccount;
            }

            // Pull all account from the cache and search for the closest match, first by HomeAccountId, and then by UPN.
            Context.Trace.WriteLine("Getting all cached accounts...");
            IReadOnlyList<IAccount> accounts = (await app.GetAccountsAsync()).ToList();
            if (accounts.Count == 0)
            {
                Context.Trace.WriteLine("No cached accounts available.");
                return null;
            }

            Context.Trace.WriteLine($"Found {accounts.Count} cached accounts.");

            Context.Trace.WriteLine($"Checking cached accounts for ID '{account.HomeAccountId}'...");
            if (!string.IsNullOrWhiteSpace(account.HomeAccountId))
            {
                IAccount byId = accounts.FirstOrDefault(a =>
                    StringComparer.OrdinalIgnoreCase.Equals(a.HomeAccountId?.Identifier, account.HomeAccountId));
                if (byId != null && !string.IsNullOrWhiteSpace(account.UserName) &&
                    !StringComparer.OrdinalIgnoreCase.Equals(byId.Username, account.UserName))
                {
                    Context.Trace.WriteLine(
                        $"Cached account UPN '{byId.Username}' differs from supplied UPN '{account.UserName}' " +
                        $"for HomeAccountId '{account.HomeAccountId}'; using HomeAccountId.");
                }

                Context.Trace.WriteLine($"Matched account by ID '{byId?.HomeAccountId}' ({byId?.Username}).)");
                return byId;
            }

            Context.Trace.WriteLine($"Checking cached accounts for UPN '{account.UserName}'...");
            if (!string.IsNullOrWhiteSpace(account.UserName))
            {
                IAccount[] matchedByName = accounts
                    .Where(a => StringComparer.OrdinalIgnoreCase.Equals(a.Username, account.UserName))
                    .ToArray();
                if (matchedByName.Length > 1)
                {
                    Context.Trace.WriteLine(
                        $"{matchedByName.Length} cached accounts share UPN '{account.UserName}'; using the first " +
                        "(provide a HomeAccountId to disambiguate).");
                }

                IAccount byName = matchedByName.FirstOrDefault();
                if (byName is not null)
                {
                    Context.Trace.WriteLine($"Matched account by UPN '{byName.HomeAccountId}' ({byName.Username}).)");
                    return byName;
                }
            }

            Context.Trace.WriteLine("No cached account found.");
            return null;
        }

        private PublicClientApplicationBuilder GetPublicAppBuilder()
        {
            if (_publicClientConfig is null)
            {
                throw new InvalidOperationException(
                    "Public client configuration is required for user authentication.");
            }

            if (_publicBuilder is null)
            {
                Context.Trace.WriteLine("Creating public client application builder...");
                _publicBuilder = PublicClientApplicationBuilder.Create(_publicClientConfig.ClientId)
                    .WithHttpClientFactory(new MsalHttpClientFactoryAdaptor(Context.HttpClientFactory))
                    .WithLegacyCacheCompatibility(false)
                    .WithDefaultRedirectUri();
            }

            return _publicBuilder;
        }

        public async Task<IEntraAuthenticationResult> GetTokenForUserAsync(
            string authority, string clientId, Uri redirectUri, string[] scopes, string userName, bool msaPt)
        {
            var uiCts = new CancellationTokenSource();

            // Check if we can and should use OS broker authentication
            bool useBroker = CanUseBroker();
            Context.Trace.WriteLine(useBroker
                ? "OS broker is available and enabled."
                : "OS broker is not available or enabled.");

            if (msaPt)
            {
                Context.Trace.WriteLine("MSA passthrough is enabled.");
            }

            try
            {
                // Create the public client application for authentication
                IPublicClientApplication app = await CreatePublicClientApplicationAsync(authority, clientId, redirectUri, useBroker, msaPt, uiCts);

                AuthenticationResult result = null;

                // Try silent authentication first if we know about an existing user
                bool hasExistingUser = !string.IsNullOrWhiteSpace(userName);
                if (hasExistingUser)
                {
                    result = await GetAccessTokenSilentlyAsync(app, scopes, userName, msaPt);
                }

                //
                // If we failed to acquire an AT silently (either because we don't have an existing user, or the user's
                // RT has expired) we need to prompt the user for credentials.
                //
                // If the user has expressed a preference in how they want to perform the interactive authentication
                // flows then we respect that. Otherwise, depending on the current platform and session type we try to
                // show the most appropriate authentication interface:
                //
                // On Windows 10+ & .NET Framework, MSAL supports the Web Account Manager (WAM) broker - we try to use
                // that if possible in the first instance.
                //
                // On .NET Framework MSAL supports the WinForms based 'embedded' webview UI. This experience is less
                // jarring that the system webview flow so try that option next.
                //
                // On other runtimes (e.g., .NET 6+) MSAL only supports the system webview flow (launch the user's
                // browser), and the device-code flows. The system webview flow requires that the redirect URI is a
                // loopback address, and that we are in an interactive session.
                //
                // The device code flow has no limitations other than a way to communicate to the user the code required
                // to authenticate.
                //
                if (result is null)
                {
                    // If the user has disabled interaction all we can do is fail at this point
                    ThrowIfUserInteractionDisabled();

                    // If we're using the OS broker then delegate everything to that
                    if (useBroker)
                    {
                        // If the user has enabled the default account feature then we can try to acquire an access
                        // token 'silently' without knowing the user's UPN. Whilst this could be done truly silently,
                        // we still prompt the user to confirm this action because if the OS account is the incorrect
                        // account then the user may become stuck in a loop of authentication failures.
                        if (!hasExistingUser && Context.Settings.UseMsAuthDefaultAccount)
                        {
                            result = await GetAccessTokenSilentlyAsync(app, scopes, null, msaPt);

                            if (result is null || !await UseDefaultAccountAsync(result.Account.Username))
                            {
                                result = null;
                            }
                        }

                        if (result is null)
                        {
                            Context.Trace.WriteLine("Performing interactive auth with broker...");
                            result = await app.AcquireTokenInteractive(scopes)
                                .WithPrompt(Prompt.SelectAccount)
                                // We must configure the system webview as a fallback
                                .WithSystemWebViewOptions(GetSystemWebViewOptions())
                                .ExecuteAsync();
                        }
                    }
                    else
                    {
                        // Check for a user flow preference if they've specified one
                        MicrosoftAuthenticationFlowType flowType = GetFlowType();
                        switch (flowType)
                        {
                            case MicrosoftAuthenticationFlowType.Auto:
                                if (CanUseEmbeddedWebView())
                                    goto case MicrosoftAuthenticationFlowType.EmbeddedWebView;

                                if (CanUseSystemWebView(app, redirectUri))
                                    goto case MicrosoftAuthenticationFlowType.SystemWebView;

                                // Fall back to device code flow
                                goto case MicrosoftAuthenticationFlowType.DeviceCode;

                            case MicrosoftAuthenticationFlowType.EmbeddedWebView:
                                Context.Trace.WriteLine("Performing interactive auth with embedded web view...");
                                EnsureCanUseEmbeddedWebView();
                                result = await app.AcquireTokenInteractive(scopes)
                                    .WithPrompt(Prompt.SelectAccount)
                                    .WithUseEmbeddedWebView(true)
                                    .WithEmbeddedWebViewOptions(GetEmbeddedWebViewOptions())
                                    .ExecuteAsync();
                                break;

                            case MicrosoftAuthenticationFlowType.SystemWebView:
                                Context.Trace.WriteLine("Performing interactive auth with system web view...");
                                EnsureCanUseSystemWebView(app, redirectUri);
                                result = await app.AcquireTokenInteractive(scopes)
                                    .WithPrompt(Prompt.SelectAccount)
                                    .WithSystemWebViewOptions(GetSystemWebViewOptions())
                                    .ExecuteAsync();
                                break;

                            case MicrosoftAuthenticationFlowType.DeviceCode:
                                Context.Trace.WriteLine("Performing interactive auth with device code...");
                                // We don't have a way to display a device code without a terminal at the moment
                                // TODO: introduce a small GUI window to show a code if no TTY exists
                                ThrowIfTerminalPromptsDisabled();
                                result = await app.AcquireTokenWithDeviceCode(scopes, ShowDeviceCodeInTty).ExecuteAsync();
                                break;

                            default:
                                goto case MicrosoftAuthenticationFlowType.Auto;
                        }
                    }
                }

                return AuthResult.FromMsalResult(result);
            }
            finally
            {
                // If we created some global UI (e.g. progress) during authentication we should dismiss them now that we're done
                uiCts.Cancel();
            }
        }

        private async Task<bool> UseDefaultAccountAsync(string userName)
        {
            ThrowIfUserInteractionDisabled();

            if (Context.SessionManager.IsDesktopSession && Context.Settings.IsGuiPromptsEnabled)
            {
                if (TryFindHelperCommand(out string command, out string args))
                {
                    var sb = new StringBuilder(args);
                    sb.Append("default-account");
                    sb.AppendFormat(" --username {0}", QuoteCmdArg(userName));

                    IDictionary<string, string> result = await InvokeHelperAsync(command, sb.ToString());

                    if (result.TryGetValue("use_default_account", out string str) && !string.IsNullOrWhiteSpace(str))
                    {
                        return str.ToBooleanyOrDefault(false);
                    }
                    else
                    {
                        throw new Trace2Exception(Context.Trace2, "Missing use_default_account in response");
                    }
                }

                var viewModel = new DefaultAccountViewModel(Context.SessionManager)
                {
                    UserName = userName
                };

                await AvaloniaUi.ShowViewAsync<DefaultAccountView>(
                    viewModel, GetParentWindowHandle(), CancellationToken.None);

                ThrowIfWindowCancelled(viewModel);

                return viewModel.UseDefaultAccount;
            }
            else
            {
                string question = $"Continue with current account ({userName})?";
                var prompt = TerminalPrompts.CreateSelection<bool>()
                    .Title(question)
                    .AddChoices(("Yes", true), ("No, use another account", false));

                return await prompt.ShowAsync(Context.Console);
            }
        }

        internal MicrosoftAuthenticationFlowType GetFlowType()
        {
            if (Context.Settings.TryGetSetting(
                Constants.EnvironmentVariables.MsAuthFlow,
                Constants.GitConfiguration.Credential.SectionName,
                Constants.GitConfiguration.Credential.MsAuthFlow,
                out string valueStr))
            {
                Context.Trace.WriteLine($"Microsoft auth flow overriden to '{valueStr}'.");
                switch (valueStr.ToLowerInvariant())
                {
                    case "auto":
                        return MicrosoftAuthenticationFlowType.Auto;
                    case "embedded":
                        return MicrosoftAuthenticationFlowType.EmbeddedWebView;
                    case "system":
                        return MicrosoftAuthenticationFlowType.SystemWebView;
                    default:
                        if (Enum.TryParse(valueStr, ignoreCase: true, out MicrosoftAuthenticationFlowType value))
                            return value;
                        break;
                }

                Context.Console.WriteWarning($"unknown Microsoft Authentication flow type '{valueStr}'; using 'auto'");
            }

            return MicrosoftAuthenticationFlowType.Auto;
        }

        /// <summary>
        /// Obtain an access token without showing UI or prompts.
        /// </summary>
        private async Task<AuthenticationResult> GetAccessTokenSilentlyAsync(
            IPublicClientApplication app, string[] scopes, string userName, bool msaPt)
        {
            try
            {
                if (userName is null)
                {
                    Context.Trace.WriteLine(
                        "Attempting to acquire token silently for current operating system account...");

                    return await app.AcquireTokenSilent(scopes, PublicClientApplication.OperatingSystemAccount)
                        .ExecuteAsync();
                }
                else
                {
                    Context.Trace.WriteLine($"Attempting to acquire token silently for user '{userName}'...");

                    // Enumerate all accounts and find the one matching the user name
                    IEnumerable<IAccount> accounts = await app.GetAccountsAsync();
                    IAccount account = accounts.FirstOrDefault(x =>
                        StringComparer.OrdinalIgnoreCase.Equals(x.Username, userName));
                    if (account is null)
                    {
                        Context.Trace.WriteLine($"No cached account found for user '{userName}'...");
                        return null;
                    }

                    var atsBuilder = app.AcquireTokenSilent(scopes, account);

                    // Is we are operating with an MSA passthrough app we need to ensure that we target the
                    // special MSA 'transfer' tenant explicitly. This is a workaround for MSAL issue:
                    // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3077
                    if (msaPt && Guid.TryParse(account.HomeAccountId.TenantId, out Guid homeTenantId) &&
                        homeTenantId == Constants.MsaHomeTenantId)
                    {
                        atsBuilder = atsBuilder.WithTenantId(Constants.MsaTransferTenantId.ToString("D"));
                    }

                    return await atsBuilder.ExecuteAsync();
                }
            }
            catch (MsalUiRequiredException)
            {
                Context.Trace.WriteLine("Failed to acquire token silently; user interaction is required.");
                return null;
            }
            catch (Exception ex)
            {
                Context.Trace.WriteLine("Failed to acquire token silently.");
                Context.Trace.WriteException(ex);
                return null;
            }
        }

        private async Task<IPublicClientApplication> CreatePublicClientApplicationAsync(string authority,
            string clientId, Uri redirectUri, bool enableBroker, bool msaPt, CancellationTokenSource uiCts)
        {
            var httpFactoryAdaptor = new MsalHttpClientFactoryAdaptor(Context.HttpClientFactory);

            var appBuilder = PublicClientApplicationBuilder.Create(clientId)
                .WithAuthority(authority)
                .WithRedirectUri(redirectUri.ToString())
                .WithHttpClientFactory(httpFactoryAdaptor);

            // Listen to MSAL logs if GCM_TRACE_MSAUTH is set
            if (Context.Settings.IsMsalTracingEnabled)
            {
                // If GCM secret tracing is enabled also enable "PII" logging in MSAL
                bool enablePiiLogging = Context.Trace.IsSecretTracingEnabled;

                appBuilder.WithLogging(OnMsalLogMessage, LogLevel.Verbose, enablePiiLogging, false);
            }

            // On Windows we should set the parent window handle for the authentication dialogs
            // so that they are displayed as a child of the correct window.
            if (PlatformUtils.IsWindows())
            {
                // If we have a parent window ID then use that, otherwise use the hosting terminal window.
                if (!string.IsNullOrWhiteSpace(Context.Settings.ParentWindowId) &&
                    int.TryParse(Context.Settings.ParentWindowId, out int hWndInt) && hWndInt > 0)
                {
                    Context.Trace.WriteLine($"Using provided parent window ID '{hWndInt}' for MSAL authentication dialogs.");
                    appBuilder.WithParentActivityOrWindow(() => new IntPtr(hWndInt));
                }
                else
                {
                    IntPtr consoleHandle = Kernel32.GetConsoleWindow();
                    IntPtr parentHandle = User32.GetAncestor(consoleHandle, GetAncestorFlags.GetRootOwner);

                    // If we don't have a console window then create a dummy top-level window (for .NET Framework)
                    // that we can use as a parent. When not on .NET Framework just use the Desktop window.
                    if (parentHandle != IntPtr.Zero)
                    {
                        Context.Trace.WriteLine($"Using console parent window ID '{parentHandle}' for MSAL authentication dialogs.");
                        appBuilder.WithParentActivityOrWindow(() => parentHandle);
                    }
                    else if (enableBroker) // Only actually need to set a parent window when using the Windows broker
                    {
                        Context.Trace.WriteLine("Using progress parent window for MSAL authentication dialogs.");
                        appBuilder.WithParentActivityOrWindow(() => ProgressWindow.ShowAndGetHandle(uiCts.Token));
                    }
                }
            }

            // Configure the broker if enabled
            // Currently only supported on Windows so only included in the .NET Framework builds
            // to save on the distribution size of the .NET builds (no need for MSALRuntime bits).
            if (enableBroker)
            {
                appBuilder.WithBroker(
                    new BrokerOptions(BrokerOptions.OperatingSystems.Windows)
                    {
                        Title = "Git Credential Manager",
                        MsaPassthrough = msaPt,
                    }
                );
            }

            IPublicClientApplication app = appBuilder.Build();

            // Register the user token cache
            await RegisterTokenCacheAsync(app.UserTokenCache, CreateUserTokenCacheProps, Context.Trace2);

            return app;
        }

        private static EmbeddedWebViewOptions GetEmbeddedWebViewOptions()
        {
            return new EmbeddedWebViewOptions
            {
                Title = "Git Credential Manager"
            };
        }

        private SystemWebViewOptions GetSystemWebViewOptions()
        {
            // TODO: add nicer HTML success and error pages
            return new SystemWebViewOptions
            {
                OpenBrowserAsync = OpenBrowserFunc
            };

            // We have special handling for Linux and WSL to open the system browser
            // so we need to use our own function here. Sorry MSAL!
            Task OpenBrowserFunc(Uri uri)
            {
                try
                {
                    Context.SessionManager.OpenBrowser(uri);
                }
                catch (Exception ex)
                {
                    Context.Trace.WriteLine("Failed to open system web browser - using MSAL fallback");
                    Context.Trace.WriteException(ex);

                    // Fallback to MSAL's default browser opening logic, preferring Edge.
                    return SystemWebViewOptions.OpenWithChromeEdgeBrowserAsync(uri);
                }

                return Task.CompletedTask;
            }
        }

        private Task ShowDeviceCodeInTty(DeviceCodeResult dcr)
        {
            Context.Console.WriteLine(dcr.Message);

            return Task.CompletedTask;
        }

        private void OnMsalLogMessage(LogLevel level, string message, bool containspii)
        {
            Context.Trace.WriteLine($"[{level.ToString()}] {message}", memberName: "MSAL");
        }

        private bool TryFindHelperCommand(out string command, out string args)
        {
            return TryFindHelperCommand(
                Constants.EnvironmentVariables.GcmUiHelper,
                Constants.GitConfiguration.Credential.UiHelper,
                Constants.DefaultUiHelper,
                out command,
                out args);
        }

        public bool CanUseBroker()
        {
            // We only support the broker on Windows 10+ and in an interactive session
            if (!PlatformUtils.IsWindowsBrokerSupported() || !Context.SessionManager.IsDesktopSession)
            {
                return false;
            }

            // Default to using the OS broker only on DevBox for the time being
            bool defaultValue = PlatformUtils.IsDevBox();

            if (Context.Settings.TryGetSetting(Constants.EnvironmentVariables.MsAuthUseBroker,
                    Constants.GitConfiguration.Credential.SectionName,
                    Constants.GitConfiguration.Credential.MsAuthUseBroker,
                    out string valueStr))
            {
                return valueStr.ToBooleanyOrDefault(defaultValue);
            }

            return defaultValue;
        }

        private bool CanUseEmbeddedWebView()
        {
            // TODO: check for desktop session once embedded web view is added back
            // return Context.SessionManager.IsDesktopSession;
            return false;
        }

        private void EnsureCanUseEmbeddedWebView()
        {
            // TODO: check for desktop session once embedded web view is added back
            // if (!Context.SessionManager.IsDesktopSession)
            // {
            //     throw new Trace2InvalidOperationException(Context.Trace2,
            //         "Embedded web view is not available without a desktop session.");
            // }

            throw new Trace2InvalidOperationException(Context.Trace2,
                "Embedded web view is not available on .NET Core.");
        }

        private bool CanUseSystemWebView(IPublicClientApplication app, Uri redirectUri)
        {
            //
            // MSAL requires the application redirect URI is a loopback address to use the System WebView
            //
            // Note: we do NOT check the MSAL 'IsSystemWebViewAvailable' property as it only
            // looks for the presence of the DISPLAY environment variable on UNIX systems.
            // This is insufficient as we instead handle launching the default browser ourselves.
            //
            return Context.SessionManager.IsWebBrowserAvailable && redirectUri.IsLoopback;
        }

        private void EnsureCanUseSystemWebView(IPublicClientApplication app, Uri redirectUri)
        {
            if (!Context.SessionManager.IsWebBrowserAvailable)
            {
                throw new Trace2InvalidOperationException(Context.Trace2,
                    "System web view is not available without a way to start a browser.");
            }

            if (!redirectUri.IsLoopback)
            {
                throw new Trace2InvalidOperationException(Context.Trace2,
                    "System web view is not available for this service configuration.");
            }
        }
    }
}
