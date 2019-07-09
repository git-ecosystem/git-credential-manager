// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Git.CredentialManager.Tests.Objects
{
    public class TestGit : IGit
    {
        public TestGitConfiguration NoRepoConfiguration { get; } = new TestGitConfiguration();

        public IDictionary<string, TestGitConfiguration> Repositories { get; } =
            new Dictionary<string, TestGitConfiguration>();

        public TestGitConfiguration AddRepository(string repoPath, IDictionary<string, string> config = null)
        {
            var repoConfig = new TestGitConfiguration
            {
                RepositoryPath = repoPath,
                Values = config ?? new Dictionary<string, string>()
            };

            Repositories.Add(repoPath, repoConfig);

            return repoConfig;
        }

        #region IGit

        void IDisposable.Dispose() { }

        IGitConfiguration IGit.GetConfiguration(string repositoryPath)
            => string.IsNullOrWhiteSpace(repositoryPath)
            ? NoRepoConfiguration
            : Repositories[repositoryPath];

        string IGit.GetRepositoryPath(string path) =>
            Repositories.Keys.FirstOrDefault(path.StartsWith);

        #endregion
    }
}
