// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

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
                // Listen to ADAL logs if GCM tracing is enabled
                if (Context.Trace.HasListeners)
                {
                    LoggerCallbackHandler.LogCallback = OnAdalLogMessage;
                }

                // If GCM secret tracing is enabled also enable "PII" logging in ADAL
                if (Context.Trace.IsSecretTracingEnabled)
                {
                    LoggerCallbackHandler.PiiLoggingEnabled = true;
                }

                IDictionary<string, string> inputDict = await Context.StdIn.ReadDictionaryAsync(StringComparer.OrdinalIgnoreCase);

                string authority   = GetArgument(inputDict, "authority");
                string clientId    = GetArgument(inputDict, "clientId");
                string redirectUri = GetArgument(inputDict, "redirectUri");
                string resource    = GetArgument(inputDict, "resource");

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

        private void OnAdalLogMessage(LogLevel level, string message, bool containspii)
        {
            Context.Trace.WriteLine($"[{level.ToString()}] {message}", memberName: "ADAL");
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
            var cache = new VisualStudioTokenCache(Context);
            var authContext = new AuthenticationContext(authority, cache);

            IPlatformParameters parameters = new PlatformParameters(PromptBehavior.SelectAccount);
            AuthenticationResult result = await authContext.AcquireTokenAsync(
                resource,
                clientId,
                redirectUri,
                parameters,
                UserIdentifier.AnyUser);

            return result.AccessToken;
        }
    }
}
