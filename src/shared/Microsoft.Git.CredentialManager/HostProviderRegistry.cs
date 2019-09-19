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
    /// <remarks>
    /// All registered <see cref="IHostProvider"/>s will be disposed when this <see cref="IHostProviderRegistry"/> is disposed.
    /// </remarks>
    public interface IHostProviderRegistry : IDisposable
    {
        /// <summary>
        /// Add the given <see cref="IHostProvider"/>(s) to this registry.
        /// </summary>
        /// <param name="hostProviders">A collection of providers to register.</param>
        /// <remarks>Providers will be disposed of when this registry instance is disposed itself.</remarks>
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
        private readonly ICommandContext _context;
        private readonly List<IHostProvider> _hostProviders;

        public HostProviderRegistry(ICommandContext context)
        {
            EnsureArgument.NotNull(context, nameof(context));

            _context = context;
            _hostProviders = new List<IHostProvider>();
        }

        public void Register(params IHostProvider[] hostProviders)
        {
            if (hostProviders == null)
            {
                throw new ArgumentNullException(nameof(hostProviders));
            }

            if (hostProviders.Any(x => StringComparer.OrdinalIgnoreCase.Equals(x.Id, Constants.ProviderIdAuto)))
            {
                throw new ArgumentException(
                    $"A host provider cannot be registered with the ID '{Constants.ProviderIdAuto}'",
                    nameof(hostProviders));
            }

            if (hostProviders.SelectMany(x => x.SupportedAuthorityIds).Any(y => StringComparer.OrdinalIgnoreCase.Equals(y, Constants.AuthorityIdAuto)))
            {
                throw new ArgumentException(
                    $"A host provider cannot be registered with the legacy authority ID '{Constants.AuthorityIdAuto}'",
                    nameof(hostProviders));
            }

            _hostProviders.AddRange(hostProviders);
        }

        public IHostProvider GetProvider(InputArguments input)
        {
            IHostProvider provider;

            //
            // Try and locate a specified provider
            //
            if (_context.Settings.ProviderOverride is string providerId)
            {
                _context.Trace.WriteLine($"Host provider override was set id='{providerId}'");

                if (!StringComparer.OrdinalIgnoreCase.Equals(Constants.ProviderIdAuto, providerId))
                {
                    provider = _hostProviders.FirstOrDefault(x => StringComparer.OrdinalIgnoreCase.Equals(x.Id, providerId));

                    if (provider is null)
                    {
                        _context.Trace.WriteLine($"No host provider was found with ID '{providerId}'.. falling back to auto-detection.");
                        _context.Streams.Error.WriteLine($"warning: a host provider override was set but no such provider '{providerId}' was found. Falling back to auto-detection.");
                    }
                    else
                    {
                        return provider;
                    }
                }
            }
            //
            // Try and locate a provider by supported authorities
            //
            else if (_context.Settings.LegacyAuthorityOverride is string authority)
            {
                _context.Trace.WriteLine($"Host provider authority override was set authority='{authority}'");
                _context.Streams.Error.WriteLine("warning: the `credential.authority` and `GCM_AUTHORITY` settings are deprecated.");
                _context.Streams.Error.WriteLine($"warning: see {Constants.HelpUrls.GcmAuthorityDeprecated} for more information.");

                if (!StringComparer.OrdinalIgnoreCase.Equals(Constants.AuthorityIdAuto, authority))
                {
                    provider = _hostProviders.FirstOrDefault(x => x.SupportedAuthorityIds.Contains(authority, StringComparer.OrdinalIgnoreCase));

                    if (provider is null)
                    {
                        _context.Trace.WriteLine($"No host provider was found with authority '{authority}'.. falling back to auto-detection.");
                        _context.Streams.Error.WriteLine($"warning: a supported authority override was set but no such provider supporting authority '{authority}' was found. Falling back to auto-detection.");
                    }
                    else
                    {
                        return provider;
                    }
                }
            }

            //
            // Auto-detection
            //
            _context.Trace.WriteLine("Performing auto-detection of host provider.");
            provider = _hostProviders.FirstOrDefault(x => x.IsSupported(input));

            if (provider is null)
            {
                throw new Exception("No host provider available to service this request.");
            }

            return provider;
        }

        public void Dispose()
        {
            // Dispose of all registered providers to give them a chance to clean up and release any resources
            foreach (IHostProvider provider in _hostProviders)
            {
                provider.Dispose();
            }
        }
    }
}
