// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Git.CredentialManager
{
    /// <summary>
    /// Represents a collection of <see cref="IHostProvider"/>s which are selected based on Git credential query
    /// <see cref="InputArguments"/>.
    /// </summary>
    public interface IHostProviderRegistry
    {
        /// <summary>
        /// Add the given <see cref="IHostProvider"/>(s) to this registry.
        /// </summary>
        /// <param name="hostProviders">A collection of providers to register.</param>
        void Register(params IHostProvider[] hostProviders);

        /// <summary>
        /// Select a <see cref="IHostProvider"/> that can service the Git credential query based on the
        /// <see cref="InputArguments"/>.
        /// </summary>
        /// <param name="input">Input arguments of a Git credential query.</param>
        /// <returns>A host provider that can service the given query.</returns>
        IHostProvider GetProvider(InputArguments input);
    }

    /// <summary>
    /// A simple host provider registry where each provider is queried in registration order until the first
    /// provider that supports the credential query is found.
    /// </summary>
    public class HostProviderRegistry : IHostProviderRegistry
    {
        private readonly List<IHostProvider> _hostProviders = new List<IHostProvider>();

        public void Register(params IHostProvider[] hostProviders)
        {
            if (hostProviders == null)
            {
                throw new ArgumentNullException(nameof(hostProviders));
            }

            _hostProviders.AddRange(hostProviders);
        }

        public IHostProvider GetProvider(InputArguments input)
        {
            var provider = _hostProviders.FirstOrDefault(x => x.IsSupported(input));

            if (provider == null)
            {
                throw new Exception("No host provider available to service this request.");
            }

            return provider;
        }
    }
}
