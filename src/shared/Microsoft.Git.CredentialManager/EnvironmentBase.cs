// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Git.CredentialManager
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
        /// <param name="system">True to update the path for all users on the system, false to update only the user's path.</param>
        void AddDirectoryToPath(string directoryPath, bool system);

        /// <summary>
        /// Remove the directory from the path.
        /// </summary>
        /// <param name="directoryPath">Path to directory to remove from the path.</param>
        /// <param name="system">True to update the path for all users on the system, false to update only the user's path.</param>
        void RemoveDirectoryFromPath(string directoryPath, bool system);
    }

    public abstract class EnvironmentBase : IEnvironment
    {
        protected EnvironmentBase(IFileSystem fileSystem)
        {
            EnsureArgument.NotNull(fileSystem, nameof(fileSystem));

            FileSystem = fileSystem;
        }

        public IReadOnlyDictionary<string, string> Variables { get; protected set; }

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

        public abstract void AddDirectoryToPath(string directoryPath, bool system);

        public abstract void RemoveDirectoryFromPath(string directoryPath, bool system);

        protected abstract string[] SplitPathVariable(string value);
    }
}
