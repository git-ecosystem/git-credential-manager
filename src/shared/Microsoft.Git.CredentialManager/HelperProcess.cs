// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager
{
    public interface IHelperProcess
    {
        /// <summary>
        /// Run a Git-helper-like process (waiting on stdout, for example)
        /// </summary>
        /// <param name="path">Path to executable</param>
        /// <param name="args">Arguments to the executable</param>
        /// <param name="standardInput">stdin to pipe into helper</param>
        /// <returns>stdout from helper executable</returns>
        Task<IDictionary<string, string>> InvokeAsync(string path, string args, IDictionary<string, string> standardInput);
    }

    public class HelperProcess : IHelperProcess
    {
        private readonly ITrace _trace;

        public HelperProcess(ITrace trace)
        {
            EnsureArgument.NotNull(trace, nameof(trace));

            _trace = trace;
        }

        public async Task<IDictionary<string, string>> InvokeAsync(string path, string args, IDictionary<string, string> standardInput = null)
        {
            var procStartInfo = new ProcessStartInfo(path)
            {
                Arguments = args,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = false, // Do not redirect stderr as tracing might be enabled
                UseShellExecute = false
            };

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
    }
}
