// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace Microsoft.Authentication.Helper
{
    public class Application : ApplicationBase
    {
        public Application(ICommandContext context)
            : base(context) { }

        protected override async Task<int> RunInternalAsync(string[] args)
        {
            try
            {
                IDictionary<string, string> inputDict = await Context.StdIn.ReadDictionaryAsync(StringComparer.OrdinalIgnoreCase);

                string authority   = GetArgument(inputDict, "authority");
                string clientId    = GetArgument(inputDict, "clientId");
                string redirectUri = GetArgument(inputDict, "redirectUri");
                string resource    = GetArgument(inputDict, "resource");
                string remoteUrl   = GetArgument(inputDict, "remoteUrl");

                // Set the remote URI to scope settings to throughout the process from now on
                Context.Settings.RemoteUri = new Uri(remoteUrl);

                string accessToken = await GetAccessTokenAsync(authority, clientId, new Uri(redirectUri), resource);

                var resultDict = new Dictionary<string, string> {["accessToken"] = accessToken};

                Context.StdOut.WriteDictionary(resultDict);

                return 0;
            }
            catch (Exception e)
            {
                var resultDict = new Dictionary<string, string> {["error"] = e.ToString()};

                Context.StdOut.WriteDictionary(resultDict);

                return -1;
            }
        }

        private void OnMsalLogMessage(LogLevel level, string message, bool containspii)
        {
            Context.Trace.WriteLine($"[{level.ToString()}] {message}", memberName: "MSAL");
        }

        private static string GetArgument(IDictionary<string, string> inputDict, string name)
        {
            if (!inputDict.TryGetValue(name, out string value))
            {
                throw new ArgumentException($"missing '{name}' input");
            }

            return value;
        }

        protected virtual async Task<string> GetAccessTokenAsync(string authority, string clientId, Uri redirectUri, string resource)
        {
            string[] scopes = { $"{resource}/.default" };

            var appBuilder = PublicClientApplicationBuilder.Create(clientId)
                                                           .WithAuthority(authority)
                                                           .WithRedirectUri(redirectUri.ToString());

            // Listen to MSAL logs if GCM_TRACE_MSAUTH is set
            if (Context.Settings.IsMsalTracingEnabled)
            {
                // If GCM secret tracing is enabled also enable "PII" logging in MSAL
                bool enablePiiLogging = Context.Trace.IsSecretTracingEnabled;

                appBuilder.WithLogging(OnMsalLogMessage, LogLevel.Verbose, enablePiiLogging, false);
            }

            IPublicClientApplication app = appBuilder.Build();

            await RegisterVSTokenCacheAsync(app);

            AuthenticationResult result = await app.AcquireTokenInteractive(scopes)
                                                   .WithPrompt(Prompt.SelectAccount)
                                                   .ExecuteAsync();

            return result.AccessToken;
        }

        private async Task RegisterVSTokenCacheAsync(IPublicClientApplication app)
        {
            Context.Trace.WriteLine("Configuring Visual Studio token cache...");

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
    }
}
