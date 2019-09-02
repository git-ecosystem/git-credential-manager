// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
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
        /// <param name="target">The level of the path environment variable that should be modified.</param>
        void AddDirectoryToPath(string directoryPath, EnvironmentVariableTarget target);

        /// <summary>
        /// Remove the directory from the path.
        /// </summary>
        /// <param name="directoryPath">Path to directory to remove from the path.</param>
        /// <param name="target">The level of the path environment variable that should be modified.</param>
        void RemoveDirectoryFromPath(string directoryPath, EnvironmentVariableTarget target);
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

        public abstract void AddDirectoryToPath(string directoryPath, EnvironmentVariableTarget target);

        public abstract void RemoveDirectoryFromPath(string directoryPath, EnvironmentVariableTarget target);

        protected abstract string[] SplitPathVariable(string value);
    }
}
