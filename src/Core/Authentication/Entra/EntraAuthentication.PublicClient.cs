using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager.Tty;
using GitCredentialManager.UI;
using GitCredentialManager.UI.ViewModels;
using GitCredentialManager.UI.Views;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using Spectre.Console;

namespace GitCredentialManager.Authentication.Entra;

public record PublicClientConfig
{
    /// <summary>
    /// Application (client) ID of the Entra application registration.
    /// </summary>
    public string ClientId { get; init; }

    /// <summary>
    /// Use legacy Microsoft Account (MSA) passthrough. Microsoft first-party applications only.
    /// </summary>
    public bool IsMsaPassthroughEnabled { get; init; }

    /// <summary>
    /// Use the shared Microsoft Developer Tools identity cache.
    /// </summary>
    /// <remarks>
    /// If <see langword="true"/> then use the token cache shared by Microsoft
    /// developer tools such as the Azure PowerShell CLI. Otherwise, use the
    /// Git Credential Manager cache, used only by GCM.
    /// </remarks>
    public bool UseSharedCache { get; init; }

    /// <summary>
    /// Indicates the client app supports the Windows Broker.
    /// </summary>
    /// <remarks>
    /// Only set this to <see langword="true"/> if the Entra application has the appropriate redirect URL:
    /// <code>ms-appx-web://microsoft.aad.brokerplugin/{clientId}</code>
    /// </remarks>
    public bool SupportsWindowsBroker { get; init; }

    /// <summary>
    /// Indicates the client app supports the Mac Broker.
    /// </summary>
    /// <remarks>
    /// Only set this to <see langword="true"/> if the Entra application has the appropriate redirect URL:
    /// <code>msauth.com.msauth.unsignedapp://auth</code>
    /// </remarks>
    public bool SupportsMacBroker { get; init; }

    /// <summary>
    /// Indicates the client app supports the Linux Broker.
    /// </summary>
    /// <remarks>
    /// Only set this to <see langword="true"/> if the Entra application has the appropriate redirect URL:
    /// <code>https://login.microsoftonline.com/common/oauth2/nativeclient</code>
    /// </remarks>
    public bool SupportsLinuxBroker { get; init; }
}

public partial class EntraAuthentication
{
    private const string MacBrokerRedirectUrl = "msauth.com.msauth.unsignedapp://auth";
    private readonly PublicClientConfig _publicClientConfig;

    public async Task<InteractionMode> GetInteractionModeAsync(CancellationToken ct = default)
    {
        // Check for broker first, because if broker will be used then we always defer to that
        // so the interaction mode doesn't actually matter!
        if (IsBrokerEnabled())
        {
            // Sadly in order to check if the broker is actually available we need to construct
            // an MSAL public client application builder, even if we're not using it at all here.
            // If the user has enabled the broker, but it is not available, there will be a small
            // performance hit calling this API. If there are subsequent calls to other public
            // client app APIs then we're just shifting the cost of the builder construction from
            // those callsites to here, since the builder is reused for all public client APIs.
            GetPublicAppBuilder(out bool useBroker);
            if (useBroker)
            {
                return InteractionMode.Auto;
            }
        }

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
            .AddChoices(available, m => m.GetDisplayName());
        return await prompt.ShowAsync(Context.Console, ct);
    }

    public async Task<IReadOnlyList<IEntraAccount>> GetUserAccountsAsync(CancellationToken ct = default)
    {
        IPublicClientApplication app = GetPublicAppBuilder(out _).Build();
        await RegisterCacheAsync(app);

        IEnumerable<IAccount> accounts = await app.GetAccountsAsync();
        return accounts.Select(EntraAccount.FromMsalAccount).ToList().AsReadOnly();
    }

    public async Task<bool> RemoveUserAccountAsync(IEntraAccount account)
    {
        IPublicClientApplication app = GetPublicAppBuilder(out _).Build();
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

    public async Task<IEntraAuthenticationResult> GetTokenForUserAsync(string[] scopes, string authority = null,
        IEntraAccount account = null, InteractionMode interactionMode = InteractionMode.Auto,
        CancellationToken ct = default)
    {
        PublicClientApplicationBuilder builder = GetPublicAppBuilder(out bool useBroker);
        if (!string.IsNullOrWhiteSpace(authority))
        {
            builder.WithAuthority(authority);
        }

        // The Windows broker always requires a parent window
        bool parentWindowRequired = useBroker && PlatformUtils.IsWindows();

        // Set up the parent window adapter to use for any interactive auth flows
        using var parentWindow = MsalParentWindowAdapter.Create(GetParentWindowHandle(), parentWindowRequired);
        builder.WithParentActivityOrWindow(parentWindow.GetWindow);

        IPublicClientApplication app = builder.Build();
        await RegisterCacheAsync(app);

        // If we've been given an account, try to resolve it to one in the cache
        IAccount msalAccount = null;
        if (account is not null)
        {
            msalAccount = await ResolveAccountAsync(app, account);
        }

        // Try silent authentication first if we have a cached account
        AuthenticationResult result = await GetTokenForUserSilentAsync(app, scopes, msalAccount, ct);
        if (result is not null)
        {
            return AuthResult.FromMsalResult(result);
        }

        ThrowIfUserInteractionDisabled();

        // Try interactive auth if we couldn't do so with a cached account
        if (useBroker)
        {
            result = await GetTokenForUserBrokerAsync(app, scopes, msalAccount, ct);
        }
        else
        {
            result = await GetTokenForUserInteractiveAsync(app, scopes, interactionMode, ct);
        }

        return AuthResult.FromMsalResult(result);
    }

    private async Task<AuthenticationResult> GetTokenForUserSilentAsync(
        IPublicClientApplication app, string[] scopes, IAccount msalAccount, CancellationToken ct)
    {
        // Silent authentication requires an account
        if (msalAccount is null)
        {
            return null;
        }

        Context.Trace.WriteLine(ReferenceEquals(msalAccount, PublicClientApplication.OperatingSystemAccount)
            ? "Attempting silent authentication using default operating system account"
            : $"Attempting silent authentication using account '{msalAccount.HomeAccountId.Identifier}'");
        try
        {
            return await app.AcquireTokenSilent(scopes, msalAccount)
                .WithMsaPassthroughTransfer(_publicClientConfig.IsMsaPassthroughEnabled, msalAccount)
                .ExecuteAsync(ct);
        }
        catch (MsalUiRequiredException)
        {
            Context.Trace.WriteLine("Silent authentication failed; interaction required!");
            return null;
        }
    }

    private async Task<AuthenticationResult> GetTokenForUserBrokerAsync(
        IPublicClientApplication app, string[] scopes, IAccount msalAccount, CancellationToken ct)
    {
        // If we don't have a specific account, let's try using the default operating system account
        // to silently authenticate first.
        if (msalAccount is null && Context.Settings.UseMsAuthDefaultAccount != false)
        {
            Context.Trace.WriteLine("Checking if default OS account can authenticate silently...");
            var result = await GetTokenForUserSilentAsync(app, scopes, PublicClientApplication.OperatingSystemAccount, ct);
            if (result is not null)
            {
                // We require the user explicitly opt in to using the default OS account
                if (Context.Settings.UseMsAuthDefaultAccount == true ||
                    await UseDefaultAccountAsync(result.Account.Username, ct))
                {
                    Context.Trace.WriteLine("Using silently acquired token for default OS account.");
                    return result;
                }

                Context.Trace.WriteLine("User opted not to use default OS account.");
            }
        }

        Context.Trace.WriteLine("Using broker for interactive authentication...");

        // On some platforms the broker requires the use of the main thread to display UI.
        // If we are on some other thread, we need to dispatch the interactive auth call to the main thread.
        bool isMainThreadRequired = PlatformUtils.IsMacOS();
        if (isMainThreadRequired && !Dispatcher.MainThread.CheckAccess())
        {
            Context.Trace.WriteLine("Dispatching interactive broker authentication to main thread...");
            Task<AuthenticationResult> mainThreadTask = await Dispatcher.MainThread.InvokeAsync(async _ =>
                await app.AcquireTokenInteractive(scopes)
                    .ExecuteAsync(ct)
            );

            return await mainThreadTask;
        }

        // Run the auth on the current thread
        return await app.AcquireTokenInteractive(scopes)
            .ExecuteAsync(ct);
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

            await AvaloniaUi.ShowViewAsync<DefaultAccountView>(viewModel, GetParentWindowHandle(), ct);

            ThrowIfWindowCancelled(viewModel);

            return viewModel.UseDefaultAccount;
        }

        var prompt = new ConfirmationPrompt($"Continue with current account ({userName})?");
        return await prompt.ShowAsync(Context.Console, ct);
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

    private Task ShowDeviceCodeAsync(DeviceCodeResult dcr)
    {
        Context.Console.WriteLine(dcr.Message);
        return Task.CompletedTask;
    }

    private PublicClientApplicationBuilder _publicBuilder;
    private bool _useBroker;

    /// <summary>
    /// Gets the public client application builder.
    /// </summary>
    /// <param name="useBroker">True if the broker will be used for this applications build using this builder.</param>
    private PublicClientApplicationBuilder GetPublicAppBuilder(out bool useBroker)
    {
        if (_publicClientConfig is null)
        {
            throw new InvalidOperationException(
                "Public client configuration is required for user authentication.");
        }

        if (_publicBuilder is null)
        {
            Context.Trace.WriteLine("Creating public client application builder...");
            var builder = PublicClientApplicationBuilder.Create(_publicClientConfig.ClientId)
                .WithHttpClientFactory(_httpFactory)
                .WithTraceLogging(Context)
                .WithLegacyCacheCompatibility(false)
                .WithDefaultRedirectUri();

            // Try and configure the broker if the user has opted in to using it,
            // and it is available in the current environment
            if (Context.SessionManager.IsDesktopSession && IsBrokerEnabled())
            {
                // Check that the app config supports the broker on this platform
                if (_publicClientConfig.SupportsWindowsBroker && PlatformUtils.IsWindows() ||
                    _publicClientConfig.SupportsMacBroker && PlatformUtils.IsMacOS() ||
                    _publicClientConfig.SupportsLinuxBroker && PlatformUtils.IsLinux())
                {
                    Context.Trace.WriteLine("Broker is supported by the app and enabled by the user.");

                    // In order to check if the broker is available you have to optimistically
                    // configure the builder to use the broker and then check.
                    builder.WithBroker(GetBrokerOptions());

                    _useBroker = builder.IsBrokerAvailable();
                    if (_useBroker)
                    {
                        Context.Trace.WriteLine("Broker authentication is available.");

                        // The macOS broker requires a specific redirect URL so the SSO
                        // extension can communicate back to our bundle.
                        // Technically this should be "msauth.{bundleID}://auth" but since
                        // we don't ship as a bundled application we must use the special
                        // "unsigned" bundle redirect URL.
                        if (PlatformUtils.IsMacOS())
                        {
                            Context.Trace.WriteLine($"Setting redirect URL for Mac broker to '{MacBrokerRedirectUrl}'");
                            builder.WithRedirectUri(MacBrokerRedirectUrl);
                        }
                    }
                    else
                    {
                        Context.Trace.WriteLine("Broker authentication is not available.");

                        // If the broker was not available we must unconfigure it, or create a new builder.
                        // Since creating a new builder is a lot of work, and there is no way to undo a
                        // "WithBroker" call (it registers the 'runtime broker' internally which cannot be undone),
                        // we just reconfigure it again but with advertised support for no supported operating
                        // systems - this effectively disables the broker again.
                        builder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.None));
                    }
                }
                else
                {
                    Context.Trace.WriteLine("Broker is not supported by the app on this platform.");
                }
            }

            _publicBuilder = builder;
        }

        useBroker = _useBroker;
        return _publicBuilder;
    }

    private BrokerOptions GetBrokerOptions()
    {
        var oses = BrokerOptions.OperatingSystems.None;

        if (_publicClientConfig.SupportsWindowsBroker)
            oses |= BrokerOptions.OperatingSystems.Windows;

        if (_publicClientConfig.SupportsMacBroker)
            oses |= BrokerOptions.OperatingSystems.OSX;

        if (_publicClientConfig.SupportsLinuxBroker)
            oses |= BrokerOptions.OperatingSystems.Linux;

        return new BrokerOptions(oses)
        {
            Title = "Git Credential Manager",
            ListOperatingSystemAccounts = true,
            MsaPassthrough = _publicClientConfig.IsMsaPassthroughEnabled
        };
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
    /// Check if the user has opted-in to using the authentication broker.
    /// </summary>
    /// <remarks>
    /// This reflects the user preference for use of the broker, and may return true
    /// even if the broker is not available on the current platform. Always check the
    /// out parameter of <see cref="GetPublicAppBuilder(out bool)"/> to see if the
    /// broker will be used for authentication.
    /// </remarks>
    private bool IsBrokerEnabled()
    {
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

    private bool TryFindHelperCommand(out string command, out string args) =>
        TryFindHelperCommand(
            Constants.EnvironmentVariables.GcmUiHelper,
            Constants.GitConfiguration.Credential.UiHelper,
            Constants.DefaultUiHelper,
            out command,
            out args);
}
