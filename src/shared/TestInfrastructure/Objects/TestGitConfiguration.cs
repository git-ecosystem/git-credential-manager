// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;

namespace Microsoft.Git.CredentialManager.Tests.Objects
{
    public class TestGitConfiguration : IGitConfiguration
    {
        public TestGitConfiguration() : this(null, null) { }

        public TestGitConfiguration(string repositoryPath) : this(repositoryPath, null) { }

        public TestGitConfiguration(IDictionary<string,string> config) : this(null, config) { }

        public TestGitConfiguration(string repositoryPath, IDictionary<string,string> config)
        {
            RepositoryPath = repositoryPath;
            Dictionary = config ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Backing dictionary for the test configuration entries.
        /// </summary>
        public IDictionary<string, string> Dictionary { get; }

        /// <summary>
        /// Convenience accessor for the backing <see cref="Dictionary"/> of configuration entries.
        /// </summary>
        /// <param name="key"></param>
        public string this[string key] { get => Dictionary[key]; set => Dictionary[key] = value; }

        #region IGitConfiguration

        public string RepositoryPath { get; set; }

        public bool TryGetValue(string name, out string value) => Dictionary.TryGetValue(name, out value);

        void IDisposable.Dispose() { }

        #endregion
    }
}
