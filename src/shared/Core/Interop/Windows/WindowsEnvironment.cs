using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace GitCredentialManager.Interop.Windows
{
    public class WindowsEnvironment : EnvironmentBase
    {
        public WindowsEnvironment(IFileSystem fileSystem)
            : base(fileSystem) { }

        internal WindowsEnvironment(IFileSystem fileSystem, IReadOnlyDictionary<string, string> variables)
            : base(fileSystem, variables) { }

        #region EnvironmentBase

        protected override string[] SplitPathVariable(string value)
        {
            // Ensure we don't return empty values here - callers may use this as the base
            // path for `Path.Combine(..)`, for which an empty value means 'current directory'.
            // We only ever want to use the current directory for path resolution explicitly.
            return value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
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
            Refresh();
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
                Refresh();
            }
        }

        #endregion

        protected override IReadOnlyDictionary<string, string> GetCurrentVariables()
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
