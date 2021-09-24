using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Microsoft.Git.CredentialManager.Interop.Windows
{
    public class WindowsEnvironment : EnvironmentBase
    {
        public WindowsEnvironment(IFileSystem fileSystem)
            : this(fileSystem, GetCurrentVariables()) { }

        internal WindowsEnvironment(IFileSystem fileSystem, IReadOnlyDictionary<string, string> variables)
            : base(fileSystem)
        {
            EnsureArgument.NotNull(variables, nameof(variables));
            Variables = variables;
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

        public override bool TryLocateExecutable(string program, out string path)
        {
            // Don't use "where.exe" on Windows as this includes the current working directory
            // and we don't want to enumerate this location; only the PATH.
            if (Variables.TryGetValue("PATH", out string pathValue))
            {
                string[] paths = SplitPathVariable(pathValue);
                foreach (var basePath in paths)
                {
                    string candidatePath = Path.Combine(basePath, program);
                    if (FileSystem.FileExists(candidatePath))
                    {
                        path = candidatePath;
                        return true;
                    }
                }
            }

            path = null;
            return false;
        }

        public override Process CreateProcess(string path, string args, bool useShellExecute, string workingDirectory)
        {
            // If we're asked to start a WSL executable we must launch via the wsl.exe command tool
            if (!useShellExecute && WslUtils.IsWslPath(path))
            {
                string wslPath = WslUtils.ConvertToDistroPath(path, out string distro);
                return WslUtils.CreateWslProcess(distro, $"{wslPath} {args}", workingDirectory);
            }

            return base.CreateProcess(path, args, useShellExecute, workingDirectory);
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
