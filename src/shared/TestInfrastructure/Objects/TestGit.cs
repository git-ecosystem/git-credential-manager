// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Git.CredentialManager.Tests.Objects
{
    public class TestGit : IGit
    {
        public TestGitConfiguration GlobalConfiguration { get; } = new TestGitConfiguration();

        public IDictionary<string, TestGitRepository> Repositories { get; } = new Dictionary<string, TestGitRepository>();

        public TestGitRepository AddRepository(TestGitRepository repo)
        {
            Repositories.Add(repo.Path, repo);
            return repo;
        }

        public TestGitRepository AddRepository(string repoPath, TestGitConfiguration repoConfig) =>
            AddRepository(new TestGitRepository(repoPath, repoConfig));

        public TestGitRepository AddRepository(string repoPath) =>
            AddRepository(repoPath, new TestGitConfiguration());

        #region IGit

        void IDisposable.Dispose() { }

        IGitConfiguration IGit.GetConfiguration(string repositoryPath)
        {
            if (string.IsNullOrWhiteSpace(repositoryPath) || !Repositories.TryGetValue(repositoryPath, out TestGitRepository repo))
            {
                return GlobalConfiguration;
            }

            IDictionary<string, IList<string>> mergedConfigDict = MergeDictionaries(GlobalConfiguration.Dictionary, repo.Configuration.Dictionary);

            return new TestGitConfiguration(mergedConfigDict);
        }

        string IGit.GetRepositoryPath(string path) => Repositories.Keys.FirstOrDefault(path.StartsWith);

        #endregion

        private static IDictionary<string, IList<string>> MergeDictionaries(params IDictionary<string, IList<string>>[] dictionaries)
        {
            var result = new Dictionary<string, IList<string>>();

            foreach (IDictionary<string, IList<string>> dict in dictionaries)
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
