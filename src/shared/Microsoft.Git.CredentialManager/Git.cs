// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;

namespace Microsoft.Git.CredentialManager
{
    public interface IGit : IDisposable
    {
        /// <summary>
        /// Get a snapshot of the configuration for the system, user, and optionally a specified repository.
        /// </summary>
        /// <param name="repositoryPath">Optional repository path from which to load local configuration.</param>
        /// <returns>Git configuration snapshot.</returns>
        IGitConfiguration GetConfiguration(string repositoryPath);

        /// <summary>
        /// Resolve the given path to a containing repository, or null if the path is not inside a Git repository.
        /// </summary>
        /// <param name="path">Path to resolve.</param>
        /// <returns>Git repository root path, or null if <paramref name="path"/> is not inside of a Git repository.</returns>
        string GetRepositoryPath(string path);
    }

    public static class GitExtensions
    {
        /// <summary>
        /// Get a snapshot of the configuration for the system and user.
        /// </summary>
        /// <param name="git">Git object.</param>
        /// <returns>Git configuration snapshot.</returns>
        public static IGitConfiguration GetConfiguration(this IGit git) => git.GetConfiguration(null);
    }
}
