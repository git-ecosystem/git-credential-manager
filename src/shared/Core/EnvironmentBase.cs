using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        /// Create a process ready to start, with redirected streams.
        /// </summary>
        /// <param name="path">Absolute file path of executable or command to start.</param>
        /// <param name="args">Command line arguments to pass to executable.</param>
        /// <param name="useShellExecute">
        /// True to resolve <paramref name="path"/> using the OS shell, false to use as an absolute file path.
        /// </param>
        /// <param name="workingDirectory">Working directory for the new process.</param>
        /// <returns><see cref="Process"/> object ready to start.</returns>
        Process CreateProcess(string path, string args, bool useShellExecute, string workingDirectory);
    }

    public abstract class EnvironmentBase : IEnvironment
    {
        protected EnvironmentBase(IFileSystem fileSystem)
        {
            EnsureArgument.NotNull(fileSystem, nameof(fileSystem));

            FileSystem = fileSystem;
        }

        public IReadOnlyDictionary<string, string> Variables { get; protected set; }

        protected ITrace Trace { get; }

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

        public abstract bool TryLocateExecutable(string program, out string path);

        public virtual Process CreateProcess(string path, string args, bool useShellExecute, string workingDirectory)
        {
            var psi = new ProcessStartInfo(path, args)
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = useShellExecute,
                WorkingDirectory = workingDirectory ?? string.Empty
            };

            return new Process { StartInfo = psi };
        }
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
        /// Create a process ready to start, with redirected streams.
        /// </summary>
        /// <param name="environment">The <see cref="IEnvironment"/>.</param>
        /// <param name="path">Absolute file path of executable or command to start.</param>
        /// <param name="args">Command line arguments to pass to executable.</param>
        /// <param name="useShellExecute">
        /// True to resolve <paramref name="path"/> using the OS shell, false to use as an absolute file path.
        /// </param>
        /// <returns><see cref="Process"/> object ready to start.</returns>
        public static Process CreateProcess(this IEnvironment environment, string path, string args, bool useShellExecute)
        {
            return environment.CreateProcess(path, args, useShellExecute, string.Empty);
        }

        /// <summary>
        /// Create a process ready to start, with redirected streams.
        /// </summary>
        /// <param name="environment">The <see cref="IEnvironment"/>.</param>
        /// <param name="path">Absolute file path of executable to start.</param>
        /// <param name="args">Command line arguments to pass to executable.</param>
        /// <returns><see cref="Process"/> object ready to start.</returns>
        public static Process CreateProcess(this IEnvironment environment, string path, string args)
        {
            return environment.CreateProcess(path, args, false, string.Empty);
        }
    }
}
