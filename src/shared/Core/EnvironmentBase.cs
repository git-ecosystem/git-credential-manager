using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GitCredentialManager
{
    /// <summary>
    /// Component that encapsulates the process environment, including environment variables.
    /// </summary>
    public interface IEnvironment
    {
        /// <summary>
        /// Current process environment variables.
        /// </summary>
        IReadOnlyDictionary<string, string> Variables { get; }

        /// <summary>
        /// Check if the given directory exists on the path.
        /// </summary>
        /// <param name="directoryPath">Path to directory to check for existence on the path.</param>
        /// <returns>True if the directory is on the path, false otherwise.</returns>
        bool IsDirectoryOnPath(string directoryPath);

        /// <summary>
        /// Add the directory to the path.
        /// </summary>
        /// <param name="directoryPath">Path to directory to add to the path.</param>
        /// <param name="target">The level of the path environment variable that should be modified.</param>
        void AddDirectoryToPath(string directoryPath, EnvironmentVariableTarget target);

        /// <summary>
        /// Remove the directory from the path.
        /// </summary>
        /// <param name="directoryPath">Path to directory to remove from the path.</param>
        /// <param name="target">The level of the path environment variable that should be modified.</param>
        void RemoveDirectoryFromPath(string directoryPath, EnvironmentVariableTarget target);

        /// <summary>
        /// Locate an executable on the current PATH.
        /// </summary>
        /// <param name="program">Executable program name.</param>
        /// <param name="path">First instance of the found executable program.</param>
        /// <returns>True if the executable was found, false otherwise.</returns>
        bool TryLocateExecutable(string program, out string path);

        /// <summary>
        /// Set an environment variable at the specified target level.
        /// </summary>
        /// <param name="variable">Name of the environment variable to set.</param>
        /// <param name="value">Value of the environment variable to set.</param>
        /// <param name="target">Target level of environment variable to set (Machine, Process, or User).</param>
        void SetEnvironmentVariable(string variable, string value,
            EnvironmentVariableTarget target = EnvironmentVariableTarget.Process);

        /// <summary>
        /// Refresh the current process environment variables. See <see cref="Variables"/>.
        /// </summary>
        /// <remarks>This is automatically called after <see cref="SetEnvironmentVariable"/>.</remarks>
        void Refresh();
    }

    public abstract class EnvironmentBase : IEnvironment
    {
        private IReadOnlyDictionary<string, string> _variables;

        protected EnvironmentBase(IFileSystem fileSystem)
        {
            EnsureArgument.NotNull(fileSystem, nameof(fileSystem));
            FileSystem = fileSystem;
        }

        internal EnvironmentBase(IFileSystem fileSystem, IReadOnlyDictionary<string, string> variables)
            : this(fileSystem)
        {
            EnsureArgument.NotNull(variables, nameof(variables));
            _variables = variables;
        }

        public IReadOnlyDictionary<string, string> Variables
        {
            get
            {
                // Variables are lazily loaded
                if (_variables is null)
                {
                    Refresh();
                }

                Debug.Assert(_variables != null);
                return _variables;
            }
        }

        protected IFileSystem FileSystem { get; }

        public bool IsDirectoryOnPath(string directoryPath)
        {
            if (Variables.TryGetValue("PATH", out string pathValue))
            {
                string[] paths = SplitPathVariable(pathValue);
                return paths.Any(x => FileSystem.IsSamePath(x, directoryPath));
            }

            return false;
        }

        public abstract void AddDirectoryToPath(string directoryPath, EnvironmentVariableTarget target);

        public abstract void RemoveDirectoryFromPath(string directoryPath, EnvironmentVariableTarget target);

        protected abstract string[] SplitPathVariable(string value);

        public virtual bool TryLocateExecutable(string program, out string path)
        {
            return TryLocateExecutable(program, null, out path);
        }

        internal virtual bool TryLocateExecutable(string program, ICollection<string> pathsToIgnore, out string path)
        {
            // On UNIX-like systems we would normally use the "which" utility to locate a program,
            // but since distributions don't always place "which" in a consistent location we cannot
            // find it! Oh the irony..
            // We could also try using "env" to then locate "which", but the same problem exists in
            // that "env" isn't always in a standard location.
            //
            // On Windows we should avoid using the equivalent utility "where.exe" because this will
            // include the current working directory in the search, and we don't want this.
            //
            // The upshot of the above means we cannot use either of "which" or "where.exe" and must
            // instead manually scan the PATH variable looking for the program.
            // At least both Windows and UNIX use the same name for the $PATH or %PATH% variable!
            if (Variables.TryGetValue("PATH", out string pathValue))
            {
                string[] paths = SplitPathVariable(pathValue);
                foreach (var basePath in paths)
                {
                    string candidatePath = Path.Combine(basePath, program);
                    if (FileSystem.FileExists(candidatePath) && (pathsToIgnore is null ||
                        !pathsToIgnore.Contains(candidatePath, StringComparer.OrdinalIgnoreCase)))
                    {
                        path = candidatePath;
                        return true;
                    }
                }
            }

            path = null;
            return false;
        }

        public void SetEnvironmentVariable(string variable, string value,
            EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
        {
            // Don't bother setting the variable if it already has the same value
            if (Variables.TryGetValue(variable, out var currentValue) &&
                StringComparer.Ordinal.Equals(currentValue, value))
            {
                return;
            }

            Environment.SetEnvironmentVariable(variable, value, target);

            // Immediately refresh the variables so that the new value is available to callers using IEnvironment
            Refresh();
        }

        public void Refresh()
        {
            _variables = GetCurrentVariables();
        }

        protected abstract IReadOnlyDictionary<string, string> GetCurrentVariables();
    }

    public static class EnvironmentExtensions
    {
        /// <summary>
        /// Locate an executable on the current PATH.
        /// </summary>
        /// <param name="environment">The <see cref="IEnvironment"/>.</param>
        /// <param name="program">Executable program name.</param>
        /// <returns>List of all instances of the found executable program, in order of most specific to least.</returns>
        public static string LocateExecutable(this IEnvironment environment, string program)
        {
            if (environment.TryLocateExecutable(program, out string path))
            {
                return path;
            }

            throw new Exception($"Failed to locate '{program}' executable on the path.");
        }

        /// <summary>
        /// Retrieves the value of an environment variable from the current process.
        /// </summary>
        /// <param name="environment">The <see cref="IEnvironment"/>.</param>
        /// <param name="variable">The name of the environment variable.</param>
        /// <returns>
        /// The value of the environment variable specified by variable, or null if the environment variable is not found.
        /// </returns>
        public static string GetEnvironmentVariable(this IEnvironment environment, string variable)
        {
            return environment.Variables.TryGetValue(variable, out string value) ? value : null;
        }
    }
}
