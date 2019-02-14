// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;

namespace Microsoft.Git.CredentialManager.Authentication
{
    public interface IMicrosoftAuthentication
    {
        Task<string> GetAccessTokenAsync(string authority, string clientId, Uri redirectUri, string resource);
    }

    public class OutOfProcHelperMicrosoftAuthentication : IMicrosoftAuthentication
    {
        private readonly ICommandContext _context;

        public OutOfProcHelperMicrosoftAuthentication(ICommandContext context)
        {
            EnsureArgument.NotNull(context, nameof(context));

            _context = context;
        }

        public async Task<string> GetAccessTokenAsync(string authority, string clientId, Uri redirectUri, string resource)
        {
            var inputDict = new Dictionary<string, string>
            {
                ["authority"]   = authority,
                ["clientId"]    = clientId,
                ["redirectUri"] = redirectUri.AbsoluteUri,
                ["resource"]    = resource,
            };

            IDictionary<string, string> resultDict = await InvokeHelperAsync(inputDict);

            if (!resultDict.TryGetValue("accessToken", out string accessToken))
            {
                throw new Exception("Missing access token in response");
            }

            return accessToken;
        }

        public async Task<IDictionary<string, string>> InvokeHelperAsync(IDictionary<string, string> input)
        {
            string helperExecutablePath = FindHelperExecutablePath();
            var procStartInfo = new ProcessStartInfo(helperExecutablePath)
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = false, // Do not redirect stderr as tracing might be enabled
                UseShellExecute = false
            };

            // We flush the trace writers here so that the we don't stomp over the
            // authentication helper's messages.
            _context.Trace.Flush();

            var process = Process.Start(procStartInfo);

            await process.StandardInput.WriteDictionaryAsync(input);

            IDictionary<string, string> resultDict = await process.StandardOutput.ReadDictionaryAsync(StringComparer.OrdinalIgnoreCase);

            await Task.Run(() => process.WaitForExit());
            int exitCode = process.ExitCode;

            if (exitCode != 0)
            {
                string errorMessage;
                if (!resultDict.TryGetValue("error", out errorMessage))
                {
                    errorMessage = "Unknown";
                }

                throw new Exception($"helper error ({exitCode}): {errorMessage}");
            }

            return resultDict;
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
            if (!_context.FileSystem.FileExists(path))
            {
                throw new Exception($"Cannot find helper '{helperName}' in '{executableDirectory}'");
            }

            return path;
        }
    }
}
