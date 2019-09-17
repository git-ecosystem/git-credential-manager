// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Microsoft.Git.CredentialManager.Authentication
{
    public interface IMicrosoftAuthentication
    {
        Task<JsonWebToken> GetAccessTokenAsync(string authority, string clientId, Uri redirectUri, string resource,
            Uri remoteUri, string userName);
    }

    public class MicrosoftAuthentication : AuthenticationBase, IMicrosoftAuthentication
    {
        public static readonly string[] AuthorityIds =
        {
            "msa",  "microsoft",   "microsoftaccount",
            "aad",  "azure",       "azuredirectory",
            "live", "liveconnect", "liveid",
        };

        public MicrosoftAuthentication(ICommandContext context)
            : base(context) {}

        #region IMicrosoftAuthentication

        public async Task<JsonWebToken> GetAccessTokenAsync(
            string authority, string clientId, Uri redirectUri, string resource, Uri remoteUri, string userName)
        {
            // If we find an external authentication helper we should delegate everything to it.
            // Assume the external helper can provide the best authentication experience.
            if (TryFindHelperExecutablePath(out string helperPath))
            {
                return await GetAccessTokenViaHelperAsync(helperPath,
                    authority, clientId, redirectUri, resource, remoteUri, userName);
            }

            // Try to acquire an access token in the current process
            string[] scopes = { $"{resource}/.default" };
            return await GetAccessTokenInProcAsync(authority, clientId, redirectUri, scopes, userName);
        }

        #endregion

        #region Authentication strategies

        /// <summary>
        /// Start an authentication helper process to obtain an access token.
        /// </summary>
        private async Task<JsonWebToken> GetAccessTokenViaHelperAsync(string helperPath,
            string authority, string clientId, Uri redirectUri, string resource, Uri remoteUri, string userName)
        {
            var inputDict = new Dictionary<string, string>
            {
                ["authority"] = authority,
                ["clientId"] = clientId,
                ["redirectUri"] = redirectUri.AbsoluteUri,
                ["resource"] = resource,
                ["remoteUrl"] = remoteUri.ToString(),
                ["username"] = userName,
            };

            IDictionary<string, string> resultDict = await InvokeHelperAsync(helperPath, null, inputDict);

            if (!resultDict.TryGetValue("accessToken", out string accessToken))
            {
                throw new Exception("Missing access token in response");
            }

            return new JsonWebToken(accessToken);
        }

        /// <summary>
        /// Obtain an access token using MSAL running inside the current process.
        /// </summary>
        private async Task<JsonWebToken> GetAccessTokenInProcAsync(string authority, string clientId, Uri redirectUri, string[] scopes, string userName)
        {
            IPublicClientApplication app = await CreatePublicClientApplicationAsync(authority, clientId, redirectUri);

            AuthenticationResult result = null;

            // Try silent authentication first if we know about an existing user
            if (!string.IsNullOrWhiteSpace(userName))
            {
                result = await GetAccessTokenSilentlyAsync(app, scopes, userName);
            }

            //
            // If we failed to acquire an AT silently (either because we don't have an existing user, or the user's RT has expired)
            // we need to prompt the user for credentials.
            //
            // Depending on the current platform and session type we try to show the most appropriate authentication interface:
            //
            // On .NET Framework MSAL supports the WinForms based 'embedded' webview UI. For Windows + .NET Framework this is the
            // best and natural experience.
            //
            // On other runtimes (e.g., .NET Core) MSAL only supports the system webview flow (launch the user's browser),
            // and the device-code flows.
            // 
            //     Note: .NET Core 3 allows using WinForms when run on Windows but MSAL does not yet support this.
            //
            // The system webview flow requires that the redirect URI is a loopback address, and that we are in an interactive session.
            //
            // The device code flow has no limitations other than a way to communicate to the user the code required to authenticate.
            //
            if (result is null)
            {
#if NETFRAMEWORK
                // If we're in an interactive session and on .NET Framework, let MSAL show the WinForms-based embeded UI
                if (PlatformUtils.IsInteractiveSession())
                {
                    result = await app.AcquireTokenInteractive(scopes)
                        .WithPrompt(Prompt.SelectAccount)
                        .WithUseEmbeddedWebView(true)
                        .ExecuteAsync();
                }
#elif NETSTANDARD
                // MSAL requires the application redirect URI is a loopback address to use the System WebView
                if (PlatformUtils.IsInteractiveSession() && app.IsSystemWebViewAvailable && redirectUri.IsLoopback)
                {
                    result = await app.AcquireTokenInteractive(scopes)
                        .WithPrompt(Prompt.SelectAccount)
                        .WithSystemWebViewOptions(GetSystemWebViewOptions())
                        .ExecuteAsync();
                }
#endif
                // If we do not have a way to show a GUI, use device code flow over the TTY
                else
                {
                    EnsureTerminalPromptsEnabled();

                    result = await app.AcquireTokenWithDeviceCode(scopes, ShowDeviceCodeInTty).ExecuteAsync();
                }
            }

            return new JsonWebToken(result.AccessToken);
        }

        /// <summary>
        /// Obtain an access token without showing UI or prompts.
        /// </summary>
        private async Task<AuthenticationResult> GetAccessTokenSilentlyAsync(IPublicClientApplication app, string[] scopes, string userName)
        {
            try
            {
                Context.Trace.WriteLine($"Attempting to acquire token silently for user '{userName}'...");

                // We can either call `app.GetAccountsAsync` and filter through the IAccount objects for the instance with the correct user name,
                // or we can just pass the user name string we have as the `loginHint` and let MSAL do exactly that for us instead!
                return await app.AcquireTokenSilent(scopes, loginHint: userName).ExecuteAsync();
            }
            catch (MsalUiRequiredException)
            {
                Context.Trace.WriteLine("Failed to acquire token silently; user interaction is required.");
                return null;
            }
        }

        private async Task<IPublicClientApplication> CreatePublicClientApplicationAsync(string authority, string clientId, Uri redirectUri)
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

            IPublicClientApplication app = appBuilder.Build();

            // Try to register the application with the VS token cache
            await RegisterVisualStudioTokenCacheAsync(app);

            return app;
        }

        #endregion

        #region Helpers

        private bool TryFindHelperExecutablePath(out string path)
        {
            string helperName = Constants.MicrosoftAuthHelperName;

            if (PlatformUtils.IsWindows())
            {
                helperName += ".exe";
            }

            string executableDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            path = Path.Combine(executableDirectory, helperName);
            return Context.FileSystem.FileExists(path);
        }

        private async Task RegisterVisualStudioTokenCacheAsync(IPublicClientApplication app)
        {
            Context.Trace.WriteLine("Configuring Visual Studio token cache...");

            // We currently only support Visual Studio on Windows
            if (PlatformUtils.IsWindows())
            {
                // The Visual Studio MSAL cache is located at "%LocalAppData%\.IdentityService\msal.cache" on Windows.
                // We use the MSAL extension library to provide us consistent cache file access semantics (synchronisation, etc)
                // as Visual Studio itself follows, as well as other Microsoft developer tools such as the Azure PowerShell CLI.
                const string cacheFileName = "msal.cache";
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string cacheDirectory = Path.Combine(appData, ".IdentityService");

                var storageProps = new StorageCreationPropertiesBuilder(cacheFileName, cacheDirectory, app.AppConfig.ClientId).Build();

                var helper = await MsalCacheHelper.CreateAsync(storageProps);
                helper.RegisterCache(app.UserTokenCache);

                Context.Trace.WriteLine("Visual Studio token cache configured.");
            }
            else
            {
                string osType = PlatformUtils.GetPlatformInformation().OperatingSystemType;
                Context.Trace.WriteLine($"Visual Studio token cache integration is not supported on {osType}.");
            }
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
    }
}
