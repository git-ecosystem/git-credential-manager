// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager.Authentication
{
    public interface IMicrosoftAuthentication
    {
        Task<string> GetAccessTokenAsync(string authority, string clientId, Uri redirectUri, string resource, Uri remoteUri);
    }

    public class MicrosoftAuthentication : AuthenticationBase, IMicrosoftAuthentication
    {
        public MicrosoftAuthentication(ICommandContext context)
            : base(context) {}

        public async Task<string> GetAccessTokenAsync(string authority, string clientId, Uri redirectUri, string resource, Uri remoteUri)
        {
            string helperPath = FindHelperExecutablePath();

            var inputDict = new Dictionary<string, string>
            {
                ["authority"]   = authority,
                ["clientId"]    = clientId,
                ["redirectUri"] = redirectUri.AbsoluteUri,
                ["resource"]    = resource,
                ["remoteUrl"]   = remoteUri.ToString(),
            };

            IDictionary<string, string> resultDict = await InvokeHelperAsync(helperPath, null, inputDict);

            if (!resultDict.TryGetValue("accessToken", out string accessToken))
            {
                throw new Exception("Missing access token in response");
            }

            return accessToken;
        }

        private string FindHelperExecutablePath()
        {
            string helperName = Constants.MicrosoftAuthHelperName;

            if (PlatformUtils.IsWindows())
            {
                helperName += ".exe";
            }

            string executableDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string path = Path.Combine(executableDirectory, helperName);
            if (!Context.FileSystem.FileExists(path))
            {
                // We expect to have a helper on Windows and Mac
                throw new Exception($"Cannot find required helper '{helperName}' in '{executableDirectory}'");
            }

            return path;
        }
    }
}
