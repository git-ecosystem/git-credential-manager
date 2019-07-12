// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager.Authentication
{
    public abstract class AuthenticationBase
    {
        protected readonly ICommandContext Context;

        protected AuthenticationBase(ICommandContext context)
        {
            EnsureArgument.NotNull(context, nameof(context));

            Context = context;
        }

        protected async Task<IDictionary<string, string>> InvokeHelperAsync(string path, string args, IDictionary<string, string> standardInput)
        {
            var procStartInfo = new ProcessStartInfo(path)
            {
                Arguments = args,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = false, // Do not redirect stderr as tracing might be enabled
                UseShellExecute = false
            };

            // We flush the trace writers here so that the we don't stomp over the
            // authentication helper's messages.
            Context.Trace.Flush();

            var process = Process.Start(procStartInfo);
            if (process is null)
            {
                throw new Exception($"Failed to start helper process '{path}'");
            }

            if (!(standardInput is null))
            {
                await process.StandardInput.WriteDictionaryAsync(standardInput);
            }

            IDictionary<string, string> resultDict = await process.StandardOutput.ReadDictionaryAsync(StringComparer.OrdinalIgnoreCase);

            await Task.Run(() => process.WaitForExit());
            int exitCode = process.ExitCode;

            if (exitCode != 0)
            {
                if (!resultDict.TryGetValue("error", out string errorMessage))
                {
                    errorMessage = "Unknown";
                }

                throw new Exception($"helper error ({exitCode}): {errorMessage}");
            }

            return resultDict;
        }

        protected void EnsureTerminalPromptsEnabled()
        {
            if (!Context.Settings.IsTerminalPromptsEnabled)
            {
                Context.Trace.WriteLine($"{Constants.EnvironmentVariables.GitTerminalPrompts} is 0; terminal prompts have been disabled.");

                throw new InvalidOperationException("Cannot show credential prompt because terminal prompts have been disabled.");
            }
        }
    }
}
