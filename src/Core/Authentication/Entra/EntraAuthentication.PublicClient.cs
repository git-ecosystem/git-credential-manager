using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager.Tty;
using Microsoft.Identity.Client;
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
}

public partial class EntraAuthentication
{
    private readonly PublicClientConfig _publicClientConfig;

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
            .AddChoices(available, m => m.GetDisplayName());
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

    public async Task<IEntraAuthenticationResult> GetTokenForUserAsync(string[] scopes, string authority = null,
        IEntraAccount account = null, InteractionMode interactionMode = InteractionMode.Auto,
        CancellationToken ct = default)
    {
        PublicClientApplicationBuilder builder = GetPublicAppBuilder();
        if (!string.IsNullOrWhiteSpace(authority))
        {
            builder.WithAuthority(authority);
        }

        // Set up the parent window adapter to use for any interactive auth flows
        MsalParentWindowAdapter parentWindow = MsalParentWindowAdapter.Create(GetParentWindowHandle());
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

        result = await GetTokenForUserInteractiveAsync(app, scopes, interactionMode, ct);

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

        Context.Trace.WriteLine(
            $"Attempting silent authentication using account '{msalAccount.HomeAccountId.Identifier}'");
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
                .WithHttpClientFactory(_httpFactory)
                .WithTraceLogging(Context)
                .WithLegacyCacheCompatibility(false)
                .WithDefaultRedirectUri();
        }

        return _publicBuilder;
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

    private bool TryFindHelperCommand(out string command, out string args) =>
        TryFindHelperCommand(
            Constants.EnvironmentVariables.GcmUiHelper,
            Constants.GitConfiguration.Credential.UiHelper,
            Constants.DefaultUiHelper,
            out command,
            out args);
}
