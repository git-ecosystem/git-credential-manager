// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Git.CredentialManager.Tests.Objects
{
    public class TestGit : IGit
    {
        public TestGit(IDictionary<string, string> globalConfig = null)
        {
            GlobalConfiguration = new TestGitConfiguration(globalConfig);
        }

        public TestGitConfiguration GlobalConfiguration { get; }

        public IDictionary<string, TestGitRepository> Repositories { get; } =
            new Dictionary<string, TestGitRepository>();

        public TestGitRepository AddRepository(string repoPath, IDictionary<string, string> config = null)
        {
            var repoConfig = new TestGitConfiguration(config);
            var repo = new TestGitRepository(repoPath, repoConfig);

            Repositories.Add(repoPath, repo);

            return repo;
        }

        #region IGit

        void IDisposable.Dispose() { }

        IGitConfiguration IGit.GetConfiguration(string repositoryPath)
        {
            if (string.IsNullOrWhiteSpace(repositoryPath) || !Repositories.TryGetValue(repositoryPath, out TestGitRepository repo))
            {
                return GlobalConfiguration;
            }

            IDictionary<string, string> mergedConfigDict = MergeDictionaries(GlobalConfiguration.Dictionary, repo.Configuration.Dictionary);

            return new TestGitConfiguration(mergedConfigDict);
        }

        string IGit.GetRepositoryPath(string path) =>
            Repositories.Keys.FirstOrDefault(path.StartsWith);

        #endregion

        private static IDictionary<string, string> MergeDictionaries(params IDictionary<string, string>[] dictionaries)
        {
            var result = new Dictionary<string, string>();

            foreach (var dict in dictionaries)
            {
                foreach (var kvp in dict)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }

            return result;
        }

        public class TestGitRepository
        {
            internal TestGitRepository(string path, TestGitConfiguration configuration)
            {
                Path = path;
                Configuration = configuration ?? new TestGitConfiguration();
            }

            public string Path { get; }

            public TestGitConfiguration Configuration { get; }
        }
    }
}
