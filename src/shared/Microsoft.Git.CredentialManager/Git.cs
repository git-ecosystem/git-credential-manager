// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager
{
    public interface IGit
    {
        /// <summary>
        /// Get the configuration for the specific configuration level.
        /// </summary>
        /// <param name="level">Configuration level filter.</param>
        /// <returns>Git configuration.</returns>
        IGitConfiguration GetConfiguration(GitConfigurationLevel level);

        /// <summary>
        /// Run a Git helper process which expects and returns key-value maps
        /// </summary>
        /// <param name="args">Arguments to the executable</param>
        /// <param name="standardInput">key-value map to pipe into stdin</param>
        /// <returns>stdout from helper executable as key-value map</returns>
        Task<IDictionary<string, string>> InvokeHelperAsync(string args, IDictionary<string, string> standardInput);
    }

    public class GitProcess : IGit
    {
        private readonly ITrace _trace;
        private readonly string _gitPath;
        private readonly string _workingDirectory;

        public GitProcess(ITrace trace, string gitPath, string workingDirectory = null)
        {
            EnsureArgument.NotNull(trace, nameof(trace));
            EnsureArgument.NotNullOrWhiteSpace(gitPath, nameof(gitPath));

            _trace = trace;
            _gitPath = gitPath;
            _workingDirectory = workingDirectory;
        }

        public IGitConfiguration GetConfiguration(GitConfigurationLevel level)
        {
            return new GitProcessConfiguration(_trace, this, level);
        }

        public Process CreateProcess(string args)
        {
            var psi = new ProcessStartInfo(_gitPath, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = _workingDirectory
            };

            return new Process {StartInfo = psi};
        }

        // This code was originally copied from 
        // src/shared/Microsoft.Git.CredentialManager/Authentication/AuthenticationBase.cs
        // That code is for GUI helpers in this codebase, while the below is for
        // communicating over Git's stdin/stdout helper protocol. The GUI helper
        // protocol will one day use a different IPC mechanism, whereas this code
        // has to follow what upstream Git does.
        public async Task<IDictionary<string, string>> InvokeHelperAsync(string args, IDictionary<string, string> standardInput = null)
        {
            var procStartInfo = new ProcessStartInfo(_gitPath)
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
                throw new Exception($"Failed to start Git helper '{args}'");
            }

            if (!(standardInput is null))
            {
                await process.StandardInput.WriteDictionaryAsync(standardInput);
                // some helpers won't continue until they see EOF
                // cf git-credential-cache
                process.StandardInput.Close();
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

    public static class GitExtensions
    {
        /// <summary>
        /// Get the configuration.
        /// </summary>
        /// <param name="git">Git object.</param>
        /// <returns>Git configuration.</returns>
        public static IGitConfiguration GetConfiguration(this IGit git) => git.GetConfiguration(GitConfigurationLevel.All);
    }
}
