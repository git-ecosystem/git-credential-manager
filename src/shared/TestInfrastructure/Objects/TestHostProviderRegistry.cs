// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager.Tests.Objects
{
    public class TestHostProviderRegistry : IHostProviderRegistry
    {
        public IHostProvider Provider { get; set; }

        #region IHostProviderRegistry

        void IHostProviderRegistry.Register(params IHostProvider[] hostProviders)
        {
        }

        Task<IHostProvider> IHostProviderRegistry.GetProviderAsync(InputArguments input)
        {
            return Task.FromResult(Provider);
        }

        #endregion

        public void Dispose()
        {
            Provider?.Dispose();
        }
    }
}
