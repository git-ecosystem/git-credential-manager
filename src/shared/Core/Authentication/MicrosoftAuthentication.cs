using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using GitCredentialManager.Interop.Windows.Native;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using System.Text;
using System.Threading;
using GitCredentialManager.UI;
using GitCredentialManager.UI.ViewModels;
using GitCredentialManager.UI.Views;

#if NETFRAMEWORK
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Identity.Client.Broker;
#endif

namespace GitCredentialManager.Authentication
{
    public interface IMicrosoftAuthentication
    {
        Task<IMicrosoftAuthenticationResult> GetTokenAsync(string authority, string clientId, Uri redirectUri,
            string[] scopes, string userName);
    }

    public interface IMicrosoftAuthenticationResult
    {
        string AccessToken { get; }
        string AccountUpn { get; }
    }

    public enum MicrosoftAuthenticationFlowType
    {
        Auto = 0,
        EmbeddedWebView = 1,
        SystemWebView = 2,
        DeviceCode = 3
    }

    public class MicrosoftAuthentication : AuthenticationBase, IMicrosoftAuthentication
    {
        public static readonly string[] AuthorityIds =
        {
            "msa",  "microsoft",   "microsoftaccount",
            "aad",  "azure",       "azuredirectory",
            "live", "liveconnect", "liveid",
        };

#if NETFRAMEWORK
        private DummyWindow _dummyWindow;
#endif

        public MicrosoftAuthentication(ICommandContext context)
            : base(context) { }

        #region IMicrosoftAuthentication

        public async Task<IMicrosoftAuthenticationResult> GetTokenAsync(
            string authority, string clientId, Uri redirectUri, string[] scopes, string userName)
        {
            // Check if we can and should use OS broker authentication
            bool useBroker = CanUseBroker();
            Context.Trace.WriteLine(useBroker
                ? "OS broker is available and enabled."
                : "OS broker is not available or enabled.");

            try
            {
                // Create the public client application for authentication
                IPublicClientApplication app = await CreatePublicClientApplicationAsync(authority, clientId, redirectUri, useBroker);

                AuthenticationResult result = null;

                // Try silent authentication first if we know about an existing user
                bool hasExistingUser = !string.IsNullOrWhiteSpace(userName);
                if (hasExistingUser)
                {
                    result = await GetAccessTokenSilentlyAsync(app, scopes, userName);
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
                            result = await GetAccessTokenSilentlyAsync(app, scopes, null);

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

                return new MsalResult(result);
            }
            finally
            {
#if NETFRAMEWORK
                // If we created a dummy window during authentication we should dispose of it now that we're done
                _dummyWindow?.Dispose();
#endif
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

                var viewModel = new DefaultAccountViewModel(Context.Environment)
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

                var menu = new TerminalMenu(Context.Terminal, question);
                TerminalMenuItem yesItem = menu.Add("Yes");
                TerminalMenuItem noItem = menu.Add("No, use another account");
                TerminalMenuItem choice = menu.Show();

                if (choice == yesItem)
                    return true;

                if (choice == noItem)
                    return false;

                throw new Exception();
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

                Context.Streams.Error.WriteLine($"warning: unknown Microsoft Authentication flow type '{valueStr}'; using 'auto'");
            }

            return MicrosoftAuthenticationFlowType.Auto;
        }

        /// <summary>
        /// Obtain an access token without showing UI or prompts.
        /// </summary>
        private async Task<AuthenticationResult> GetAccessTokenSilentlyAsync(IPublicClientApplication app, string[] scopes, string userName)
        {
            try
            {
                if (userName is null)
                {
                    Context.Trace.WriteLine("Attempting to acquire token silently for current operating system account...");

                    return await app.AcquireTokenSilent(scopes, PublicClientApplication.OperatingSystemAccount).ExecuteAsync();
                }
                else
                {
                    Context.Trace.WriteLine($"Attempting to acquire token silently for user '{userName}'...");

                    // We can either call `app.GetAccountsAsync` and filter through the IAccount objects for the instance with the correct user name,
                    // or we can just pass the user name string we have as the `loginHint` and let MSAL do exactly that for us instead!
                    return await app.AcquireTokenSilent(scopes, loginHint: userName).ExecuteAsync();
                }
            }
            catch (MsalUiRequiredException)
            {
                Context.Trace.WriteLine("Failed to acquire token silently; user interaction is required.");
                return null;
            }
        }

        private async Task<IPublicClientApplication> CreatePublicClientApplicationAsync(
            string authority, string clientId, Uri redirectUri, bool enableBroker)
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
#if NETFRAMEWORK
                        Context.Trace.WriteLine($"Using dummy parent window for MSAL authentication dialogs.");
                        _dummyWindow = new DummyWindow();
                        appBuilder.WithParentActivityOrWindow(_dummyWindow.ShowAndGetHandle);
#endif
                    }
                }
            }

            // Configure the broker if enabled
            // Currently only supported on Windows so only included in the .NET Framework builds
            // to save on the distribution size of the .NET builds (no need for MSALRuntime bits).
            if (enableBroker)
            {
#if NETFRAMEWORK
                appBuilder.WithBroker(
                    new BrokerOptions(BrokerOptions.OperatingSystems.Windows)
                    {
                        Title = "Git Credential Manager",
                        MsaPassthrough = true,
                    }
                );
#endif
            }

            IPublicClientApplication app = appBuilder.Build();

            // Register the application token cache
            await RegisterTokenCacheAsync(app, Context.Trace2);

            return app;
        }

        #endregion

        #region Helpers

        private async Task RegisterTokenCacheAsync(IPublicClientApplication app, ITrace2 trace2)
        {
            Context.Trace.WriteLine(
                "Configuring Microsoft Authentication token cache to instance shared with Microsoft developer tools...");

            if (!PlatformUtils.IsWindows() && !PlatformUtils.IsPosix())
            {
                string osType = PlatformUtils.GetPlatformInformation(trace2).OperatingSystemType;
                Context.Trace.WriteLine($"Token cache integration is not supported on {osType}.");
                return;
            }

            // We use the MSAL extension library to provide us consistent cache file access semantics (synchronisation, etc)
            // as other Microsoft developer tools such as the Azure PowerShell CLI.
            MsalCacheHelper helper = null;
            try
            {
                var storageProps = CreateTokenCacheProps(useLinuxFallback: false);
                helper = await MsalCacheHelper.CreateAsync(storageProps);

                // Test that cache access is working correctly
                helper.VerifyPersistence();
            }
            catch (MsalCachePersistenceException ex)
            {
                var message = "Cannot persist Microsoft Authentication data securely!";
                Context.Streams.Error.WriteLine("warning: cannot persist Microsoft authentication token cache securely!");
                Context.Trace.WriteLine(message);
                Context.Trace.WriteException(ex);
                Context.Trace2.WriteError(message);

                if (PlatformUtils.IsMacOS())
                {
                    // On macOS sometimes the Keychain returns the "errSecAuthFailed" error - we don't know why
                    // but it appears to be something to do with not being able to access the keychain.
                    // Locking and unlocking (or restarting) often fixes this.
                    Context.Streams.Error.WriteLine(
                        "warning: there is a problem accessing the login Keychain - either manually lock and unlock the " +
                        "login Keychain, or restart the computer to remedy this");
                }
                else if (PlatformUtils.IsLinux())
                {
                    // On Linux the SecretService/keyring might not be available so we must fall-back to a plaintext file.
                    Context.Streams.Error.WriteLine("warning: using plain-text fallback token cache");
                    Context.Trace.WriteLine("Using fall-back plaintext token cache on Linux.");
                    var storageProps = CreateTokenCacheProps(useLinuxFallback: true);
                    helper = await MsalCacheHelper.CreateAsync(storageProps);
                }
            }

            if (helper is null)
            {
                Context.Streams.Error.WriteLine("error: failed to set up Microsoft Authentication token cache!");
                Context.Trace.WriteLine("Failed to integrate with shared token cache!");
            }
            else
            {
                helper.RegisterCache(app.UserTokenCache);
                Context.Trace.WriteLine("Microsoft developer tools token cache configured.");
            }
        }

        internal StorageCreationProperties CreateTokenCacheProps(bool useLinuxFallback)
        {
            const string cacheFileName = "msal.cache";
            string cacheDirectory;
            if (PlatformUtils.IsWindows())
            {
                // The shared MSAL cache is located at "%LocalAppData%\.IdentityService\msal.cache" on Windows.
                cacheDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    ".IdentityService"
                );
            }
            else
            {
                // The shared MSAL cache metadata is located at "~/.local/.IdentityService/msal.cache" on UNIX.
                cacheDirectory = Path.Combine(Context.FileSystem.UserHomePath, ".local", ".IdentityService");
            }

            // The keychain is used on macOS with the following service & account names
            var builder = new StorageCreationPropertiesBuilder(cacheFileName, cacheDirectory)
                .WithMacKeyChain("Microsoft.Developer.IdentityService", "MSALCache");

            if (useLinuxFallback)
            {
                builder.WithLinuxUnprotectedFile();
            }
            else
            {
                // The SecretService/keyring is used on Linux with the following collection name and attributes
                builder.WithLinuxKeyring(cacheFileName,
                    "default", "MSALCache",
                    new KeyValuePair<string, string>("MsalClientID", "Microsoft.Developer.IdentityService"),
                    new KeyValuePair<string, string>("Microsoft.Developer.IdentityService", "1.0.0.0"));
            }

            return builder.Build();
        }

        private static EmbeddedWebViewOptions GetEmbeddedWebViewOptions()
        {
            return new EmbeddedWebViewOptions
            {
                Title = "Git Credential Manager"
            };
        }

        private static SystemWebViewOptions GetSystemWebViewOptions()
        {
            // TODO: add nicer HTML success and error pages
            return new SystemWebViewOptions();
        }

        private Task ShowDeviceCodeInTty(DeviceCodeResult dcr)
        {
            Context.Terminal.WriteLine(dcr.Message);

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

        private class MsalHttpClientFactoryAdaptor : IMsalHttpClientFactory
        {
            private readonly IHttpClientFactory _factory;
            private HttpClient _instance;

            public MsalHttpClientFactoryAdaptor(IHttpClientFactory factory)
            {
                EnsureArgument.NotNull(factory, nameof(factory));

                _factory = factory;
            }

            public HttpClient GetHttpClient()
            {
                // MSAL calls this method each time it wants to use an HTTP client.
                // We ensure we only create a single instance to avoid socket exhaustion.
                return _instance ?? (_instance = _factory.CreateClient());
            }
        }

        #endregion

        #region Auth flow capability detection

        public bool CanUseBroker()
        {
#if NETFRAMEWORK
            // We only support the broker on Windows 10+ and in an interactive session
            if (!Context.SessionManager.IsDesktopSession || !PlatformUtils.IsWindowsBrokerSupported())
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
#else
            // OS broker requires .NET Framework right now until we migrate to .NET 5.0 (net5.0-windows10.x.y.z)
            return false;
#endif
        }

        private bool CanUseEmbeddedWebView()
        {
            // If we're in an interactive session and on .NET Framework then MSAL can show the WinForms-based embedded UI
#if NETFRAMEWORK
            return Context.SessionManager.IsDesktopSession;
#else
            return false;
#endif
        }

        private void EnsureCanUseEmbeddedWebView()
        {
#if NETFRAMEWORK
            if (!Context.SessionManager.IsDesktopSession)
            {
                throw new Trace2InvalidOperationException(Context.Trace2,
                    "Embedded web view is not available without a desktop session.");
            }
#else
            throw new Trace2InvalidOperationException(Context.Trace2,
                "Embedded web view is not available on .NET Core.");
#endif
        }

        private bool CanUseSystemWebView(IPublicClientApplication app, Uri redirectUri)
        {
            // MSAL requires the application redirect URI is a loopback address to use the System WebView
            return Context.SessionManager.IsWebBrowserAvailable && app.IsSystemWebViewAvailable && redirectUri.IsLoopback;
        }

        private void EnsureCanUseSystemWebView(IPublicClientApplication app, Uri redirectUri)
        {
            if (!Context.SessionManager.IsWebBrowserAvailable)
            {
                throw new Trace2InvalidOperationException(Context.Trace2,
                    "System web view is not available without a way to start a browser.");
            }

            if (!app.IsSystemWebViewAvailable)
            {
                throw new Trace2InvalidOperationException(Context.Trace2,
                    "System web view is not available on this platform.");
            }

            if (!redirectUri.IsLoopback)
            {
                throw new Trace2InvalidOperationException(Context.Trace2,
                    "System web view is not available for this service configuration.");
            }
        }

        #endregion

        private class MsalResult : IMicrosoftAuthenticationResult
        {
            private readonly AuthenticationResult _msalResult;

            public MsalResult(AuthenticationResult msalResult)
            {
                _msalResult = msalResult;
            }

            public string AccessToken => _msalResult.AccessToken;
            public string AccountUpn => _msalResult.Account.Username;
        }

#if NETFRAMEWORK
        private class DummyWindow : IDisposable
        {
            private readonly Thread _staThread;
            private readonly ManualResetEventSlim _readyEvent;
            private Form _window;
            private IntPtr _handle;

            public DummyWindow()
            {
                _staThread = new Thread(ThreadProc);
                _staThread.SetApartmentState(ApartmentState.STA);
                _readyEvent = new ManualResetEventSlim();
            }

            public IntPtr ShowAndGetHandle()
            {
                _staThread.Start();
                _readyEvent.Wait();
                return _handle;
            }

            public void Dispose()
            {
                _window?.Invoke(() => _window.Close());

                if (_staThread.IsAlive)
                {
                    _staThread.Join();
                }
            }

            private void ThreadProc()
            {
                System.Windows.Forms.Application.EnableVisualStyles();
                _window = new Form
                {
                    TopMost = true,
                    ControlBox = false,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    ClientSize = new Size(182, 46),
                    FormBorderStyle = FormBorderStyle.None,
                    StartPosition = FormStartPosition.CenterScreen,
                };

                var progress = new ProgressBar
                {
                    Style = ProgressBarStyle.Marquee,
                    Location = new Point(12, 12),
                    Size = new Size(158, 23),
                    MarqueeAnimationSpeed = 30,
                };

                _window.Controls.Add(progress);
                _window.Shown += (s, e) =>
                {
                    _handle = _window.Handle;
                    _readyEvent.Set();
                };

                _window.ShowDialog();
                _window.Dispose();
                _window = null;
            }
        }
#endif
    }
}
