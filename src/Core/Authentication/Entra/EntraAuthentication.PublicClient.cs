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

    public partial class EntraAuthentication
    {
        private readonly PublicClientConfig _publicClientConfig;
        private PublicClientApplicationBuilder _publicBuilder;

        public async Task<InteractionMode> GetInteractionModeAsync(CancellationToken ct = default)
        {
            // Check for a stored user preference
            if (TryGetModePreference(out InteractionMode mode))
            {
                return mode;
            }

            // Determine the set of available modes
            IList<InteractionMode> available = GetAvailableModes();

            // Show auth mode prompt
            if (Context.Settings.IsGuiPromptsEnabled && Context.SessionManager.IsDesktopSession)
            {
                if (TryFindHelperCommand(out string command, out string args))
                {
                    var availableNames = available.Select(m => m.ToString().ToLowerInvariant());

                    var sb = new StringBuilder(args);
                    sb.Append("select-interaction-mode");
                    sb.AppendFormat(" --available {0}", QuoteCmdArg(string.Join(',', availableNames)));

                    IDictionary<string, string> result = await InvokeHelperAsync(command, sb.ToString());
                    if (result.TryGetValue("interaction_mode", out string str) &&
                        Enum.TryParse(str, ignoreCase: true, out InteractionMode choice))
                    {
                        return choice;
                    }

                    throw new Trace2Exception(Context.Trace2, "Missing or invalid interaction_mode in response");
                }

                // TODO: show prompt in-proc
            }

            // Show prompt in tty
            var prompt = TerminalPrompts.CreateSelection<InteractionMode>()
                .Title("Select an authentication flow")
                .AddChoices(available.Select(m => (m.GetDisplayName(), m)).ToArray());
            return await prompt.ShowAsync(Context.Console, ct);
        }

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
            string authority, string clientId, Uri redirectUri, string[] scopes, IEntraAccount account, bool msaPt,
            InteractionMode interactionMode = InteractionMode.Auto, CancellationToken ct = default)
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

                // If we've been given an account, try to resolve it to one in the cache
                IAccount msalAccount = null;
                if (account is not null)
                {
                    msalAccount = await ResolveAccountAsync(app, account);
                }

                AuthenticationResult result = null;

                // Try silent authentication first if we know about an existing user
                bool hasExistingUser = msalAccount is not null;
                if (hasExistingUser)
                {
                    result = await GetAccessTokenSilentlyAsync(app, scopes, msalAccount, msaPt, ct);
                }

                //
                // If we failed to acquire an AT silently (either because we don't have an existing user, or the user's
                // RT has expired) we need to prompt the user for credentials.
                //
                // If we're using the OS broker then delegate everything to that. Otherwise, ask for (or use a stored
                // preference for) the interaction mode to use, and use that to select the most appropriate
                // authentication interface for the current platform and session type.
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
                        if (!hasExistingUser && Context.Settings.UseMsAuthDefaultAccount != false)
                        {
                            result = await GetAccessTokenSilentlyAsync(app, scopes, null, msaPt, ct);

                            if (result is null || !await UseDefaultAccountAsync(result.Account.Username, ct))
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
                                .ExecuteAsync(ct);
                        }
                    }
                    else
                    {
                        result = await GetTokenForUserInteractiveAsync(app, scopes, interactionMode, ct);
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

        private async Task<AuthenticationResult> GetTokenForUserInteractiveAsync(
            IPublicClientApplication app, string[] scopes, InteractionMode interactionMode, CancellationToken ct)
        {
            // Check for a stored preference if we've not been given a specific mode from the caller
            if (interactionMode == InteractionMode.Auto && TryGetModePreference(out InteractionMode mode))
            {
                Context.Trace.WriteLine($"Interaction mode overriden to '{mode}'.");
                interactionMode = mode;
            }

            switch (interactionMode)
            {
                // Try to use the most appropriate interaction mode available
                case InteractionMode.Auto:
                    Context.Trace.WriteLine("Resolving interactive mode auto...");
                    if (IsEmbeddedWebViewAvailable())
                        goto case InteractionMode.EmbeddedWebView;

                    if (IsSystemWebViewAvailable())
                        goto case InteractionMode.SystemWebView;

                    if (IsDeviceCodeAvailable())
                        goto case InteractionMode.DeviceCode;

                    throw new InvalidOperationException("No available interaction modes.");

                case InteractionMode.EmbeddedWebView:
                    Context.Trace.WriteLine("Performing interactive authentication via embedded webview...");
                    return await app.AcquireTokenInteractive(scopes)
                        .WithUseEmbeddedWebView(true)
                        .WithEmbeddedWebViewOptions(GetEmbeddedWebViewOptions())
                        .ExecuteAsync(ct);

                case InteractionMode.SystemWebView:
                    Context.Trace.WriteLine("Performing interactive authentication via system webview...");
                    Context.Console.WriteInfo("opening browser to complete authentication...");
                    return await app.AcquireTokenInteractive(scopes)
                        .WithUseEmbeddedWebView(false)
                        .WithSystemWebViewOptions(GetSystemWebViewOptions())
                        .ExecuteAsync(ct);

                case InteractionMode.DeviceCode:
                    Context.Trace.WriteLine("Performing interactive authentication via device code...");
                    // We don't have a way to display a device code without a terminal at the moment
                    // TODO: introduce a small GUI window to show a code if no TTY exists
                    ThrowIfTerminalPromptsDisabled();
                    return await app.AcquireTokenWithDeviceCode(scopes, ShowDeviceCodeAsync)
                        .ExecuteAsync(ct);

                default:
                    throw new ArgumentOutOfRangeException(nameof(interactionMode), interactionMode, "Unexpected interaction mode.");
            }
        }

        private async Task<bool> UseDefaultAccountAsync(string userName, CancellationToken ct)
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

                    throw new Trace2Exception(Context.Trace2, "Missing use_default_account in response");
                }

                var viewModel = new DefaultAccountViewModel(Context.SessionManager)
                {
                    UserName = userName
                };

                await AvaloniaUi.ShowViewAsync<DefaultAccountView>(
                    viewModel, GetParentWindowHandle(), ct);

                ThrowIfWindowCancelled(viewModel);

                return viewModel.UseDefaultAccount;
            }

            var prompt = new ConfirmationPrompt($"Continue with current account ({userName})?");
            return await prompt.ShowAsync(Context.Console, ct);
        }

        private IList<InteractionMode> GetAvailableModes()
        {
            var list = new List<InteractionMode> { InteractionMode.Auto };
            if (IsEmbeddedWebViewAvailable())
            {
                list.Add(InteractionMode.EmbeddedWebView);
            }
            if (IsSystemWebViewAvailable())
            {
                list.Add(InteractionMode.SystemWebView);
            }
            if (IsDeviceCodeAvailable())
            {
                list.Add(InteractionMode.DeviceCode);
            }
            return list;
        }

        private bool TryGetModePreference(out InteractionMode mode)
        {
            if (Context.Settings.TryGetSetting(
                    Constants.EnvironmentVariables.MsAuthFlow,
                    Constants.GitConfiguration.Credential.SectionName,
                    Constants.GitConfiguration.Credential.MsAuthFlow,
                    out string valueStr))
            {
                Context.Trace.WriteLine($"Interaction mode overriden to '{valueStr}'.");
                switch (valueStr.ToLowerInvariant())
                {
                    case "auto":
                        mode = InteractionMode.Auto;
                        break;
                    case "embedded":
                        mode = InteractionMode.EmbeddedWebView;
                        break;
                    case "system":
                        mode = InteractionMode.SystemWebView;
                        break;
                    case "device":
                        mode = InteractionMode.DeviceCode;
                        break;
                    default:
                        if (!Enum.TryParse(valueStr, ignoreCase: true, out mode))
                        {
                            Context.Console.WriteWarning($"unknown interaction mode '{valueStr}'; using 'auto'");
                            mode = InteractionMode.Auto;
                        }
                        break;
                }
                return true;
            }

            mode = InteractionMode.Auto;
            return false;
        }

        /// <summary>
        /// Obtain an access token without showing UI or prompts.
        /// </summary>
        private async Task<AuthenticationResult> GetAccessTokenSilentlyAsync(
            IPublicClientApplication app, string[] scopes, IAccount account, bool msaPt, CancellationToken ct = default)
        {
            try
            {
                if (account is null)
                {
                    Context.Trace.WriteLine(
                        "Attempting to acquire token silently for current operating system account...");

                    return await app.AcquireTokenSilent(scopes, PublicClientApplication.OperatingSystemAccount)
                        .ExecuteAsync(ct);
                }
                else
                {
                    Context.Trace.WriteLine($"Attempting to acquire token silently for account '{account.HomeAccountId?.Identifier}'...");

                    var atsBuilder = app.AcquireTokenSilent(scopes, account);

                    // Is we are operating with an MSA passthrough app we need to ensure that we target the
                    // special MSA 'transfer' tenant explicitly. This is a workaround for MSAL issue:
                    // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3077
                    if (msaPt && Guid.TryParse(account.HomeAccountId.TenantId, out Guid homeTenantId) &&
                        homeTenantId == Constants.MsaHomeTenantId)
                    {
                        atsBuilder = atsBuilder.WithTenantId(Constants.MsaTransferTenantId.ToString("D"));
                    }

                    return await atsBuilder.ExecuteAsync(ct);
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
            await RegisterCacheAsync(app);

            return app;
        }

        private EmbeddedWebViewOptions GetEmbeddedWebViewOptions()
        {
            return new EmbeddedWebViewOptions
            {
                Title = "Git Credential Manager"
            };
        }

        private SystemWebViewOptions GetSystemWebViewOptions()
        {
            return new SystemWebViewOptions
            {
                OpenBrowserAsync = uri =>
                {
                    try
                    {
                        Context.SessionManager.OpenBrowser(uri);
                        return Task.CompletedTask;
                    }
                    catch (Exception ex)
                    {
                        Context.Trace.WriteLine("Failed to open system web browser - using MSAL fallback");
                        Context.Trace.WriteException(ex);
                        return SystemWebViewOptions.OpenWithChromeEdgeBrowserAsync(uri);
                    }
                }
            };
        }

        private Task ShowDeviceCodeAsync(DeviceCodeResult dcr)
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

        private bool IsEmbeddedWebViewAvailable() =>
            // TODO: check for desktop session once embedded web view is added back
            // return Context.SessionManager.IsDesktopSession;
            false;

        private bool IsSystemWebViewAvailable() => Context.SessionManager.IsWebBrowserAvailable;

        private bool IsDeviceCodeAvailable() => Context.Settings.IsTerminalPromptsEnabled;
    }
}
