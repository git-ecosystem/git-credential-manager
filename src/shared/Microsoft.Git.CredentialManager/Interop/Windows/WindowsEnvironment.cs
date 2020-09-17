// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.Git.CredentialManager.Interop.Windows
{
    public class WindowsEnvironment : EnvironmentBase
    {
        public WindowsEnvironment(IFileSystem fileSystem) : base(fileSystem)
        {
            Variables = GetCurrentVariables();
        }

        #region EnvironmentBase

        protected override string[] SplitPathVariable(string value)
        {
            return value.Split(';');
        }

        public override void AddDirectoryToPath(string directoryPath, EnvironmentVariableTarget target)
        {
            // Read the current PATH variable, not the cached one
            string currentValue = Environment.GetEnvironmentVariable("PATH", target) ?? string.Empty;

            // Append directory to the end
            var sb = new StringBuilder();
            sb.Append(currentValue);
            if (!currentValue.EndsWith(";"))
            {
                sb.Append(';');
            }
            sb.Append(directoryPath);

            string newValue = sb.ToString();

            // Update the real system immediately
            Environment.SetEnvironmentVariable("PATH", newValue, target);

            // Update the cached PATH variable to the latest value (as well as all other variables)
            Variables = GetCurrentVariables();
        }

        public override void RemoveDirectoryFromPath(string directoryPath, EnvironmentVariableTarget target)
        {
            // Read the current PATH variable, not the cached one
            string currentValue = Environment.GetEnvironmentVariable("PATH", target) ?? string.Empty;

            // Only need to update PATH if it does indeed contain the parent directory
            if (directoryPath != null && currentValue.IndexOf(directoryPath, StringComparison.OrdinalIgnoreCase) > -1)
            {
                // Cut out the directory path
                string newValue = currentValue.TrimMiddle(directoryPath, StringComparison.OrdinalIgnoreCase);

                // Update the real system immediately
                Environment.SetEnvironmentVariable("PATH", newValue, target);

                // Update the cached PATH variable to the latest value (as well as all other variables)
                Variables = GetCurrentVariables();
            }
        }

        public override string LocateExecutable(string program)
        {
            string wherePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "where.exe");
            var psi = new ProcessStartInfo(wherePath, program)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            using (var where = new Process {StartInfo = psi})
            {
                where.Start();
                where.WaitForExit();

                if (where.ExitCode != 0)
                {
                    throw new Exception($"Failed to locate '{program}' using where.exe. Exit code: {where.ExitCode}.");
                }

                string stdout = where.StandardOutput.ReadToEnd();
                if (string.IsNullOrWhiteSpace(stdout))
                {
                    return null;
                }

                string[] results = stdout.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
                return results.FirstOrDefault();
            }
        }

        #endregion

        private static IReadOnlyDictionary<string, string> GetCurrentVariables()
        {
            // On Windows it is technically possible to get env vars which differ only by case
            // even though the general assumption is that they are case insensitive on Windows.
            // For example, some of the standard .NET types like System.Diagnostics.Process
            // will fail to start a process on Windows if given duplicate environment variables.
            // See this issue for more information: https://github.com/dotnet/corefx/issues/13146

            // We should de-duplicate by setting the string comparer to OrdinalIgnoreCase.
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var variables = Environment.GetEnvironmentVariables();

            foreach (var key in variables.Keys)
            {
                if (key is string name && variables[key] is string value)
                {
                    dict[name] = value;
                }
            }

            return dict;
        }
    }
}
