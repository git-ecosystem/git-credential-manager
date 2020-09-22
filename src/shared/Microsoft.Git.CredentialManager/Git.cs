// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Diagnostics;

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
