using System;
using System.Collections.Generic;

namespace Microsoft.Git.CredentialManager.Tests.Objects
{
    public class TestGitConfiguration : IGitConfiguration
    {
        public string RepositoryPath { get; set; }

        public IDictionary<string, string> Values { get; set; } = new Dictionary<string, string>();

        #region IGitConfiguration

        string IGitConfiguration.RepositoryPath => RepositoryPath;

        bool IGitConfiguration.TryGetString(string name, out string value) => Values.TryGetValue(name, out value);

        void IDisposable.Dispose() { }

        #endregion
    }
}
