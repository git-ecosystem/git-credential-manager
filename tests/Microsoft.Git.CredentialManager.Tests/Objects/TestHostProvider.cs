// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager.Tests.Objects
{
    public class TestHostProvider : IHostProvider
    {
        public string Name { get; set; } = "TestProvider";

        public bool IsSupported { get; set; } = true;

        public string CredentialKey { get; set; }

        public GitCredential Credential { get; set; }

        #region IHostProvider

        string IHostProvider.Name => Name;

        bool IHostProvider.IsSupported(InputArguments input) => IsSupported;

        string IHostProvider.GetCredentialKey(InputArguments input) => CredentialKey;

        Task<GitCredential> IHostProvider.CreateCredentialAsync(InputArguments input) => Task.FromResult(Credential);

        #endregion
    }
}
