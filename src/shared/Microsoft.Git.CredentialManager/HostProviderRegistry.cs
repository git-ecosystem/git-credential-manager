// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager
{
    /// <summary>
    /// Priority in which host providers are queried during auto-detection.
    /// </summary>
    public enum HostProviderPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
    }

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
        /// Add the given <see cref="IHostProvider"/> to this registry.
        /// </summary>
        /// <param name="hostProvider">Host provider to register.</param>
        /// <param name="priority">Priority at which the provider will be considered when auto-detecting.</param>
        /// <remarks>Providers will be disposed of when this registry instance is disposed itself.</remarks>
        void Register(IHostProvider hostProvider, HostProviderPriority priority);

        /// <summary>
        /// Select a <see cref="IHostProvider"/> that can service the Git credential query based on the
        /// <see cref="InputArguments"/>.
        /// </summary>
        /// <param name="input">Input arguments of a Git credential query.</param>
        /// <returns>A host provider that can service the given query.</returns>
        Task<IHostProvider> GetProviderAsync(InputArguments input);
    }

    /// <summary>
    /// Host provider registry where each provider is queried by priority order until the first
    /// provider that supports the credential query or matches the endpoint query is found.
    /// </summary>
    public class HostProviderRegistry : IHostProviderRegistry
    {
        private readonly ICommandContext _context;
        private readonly IDictionary<HostProviderPriority, ICollection<IHostProvider>> _hostProviders;

        public HostProviderRegistry(ICommandContext context)
        {
            EnsureArgument.NotNull(context, nameof(context));

            _context = context;
            _hostProviders = new Dictionary<HostProviderPriority, ICollection<IHostProvider>>();
        }

        public void Register(IHostProvider hostProvider, HostProviderPriority priority)
        {
            EnsureArgument.NotNull(hostProvider, nameof(hostProvider));

            if (StringComparer.OrdinalIgnoreCase.Equals(hostProvider.Id, Constants.ProviderIdAuto))
            {
                throw new ArgumentException(
                    $"A host provider cannot be registered with the ID '{Constants.ProviderIdAuto}'",
                    nameof(hostProvider));
            }

            if (hostProvider.SupportedAuthorityIds.Any(y => StringComparer.OrdinalIgnoreCase.Equals(y, Constants.AuthorityIdAuto)))
            {
                throw new ArgumentException(
                    $"A host provider cannot be registered with the legacy authority ID '{Constants.AuthorityIdAuto}'",
                    nameof(hostProvider));
            }

            if (!_hostProviders.TryGetValue(priority, out ICollection<IHostProvider> providers))
            {
                providers = new List<IHostProvider>();
                _hostProviders[priority] = providers;
            }

            providers.Add(hostProvider);
        }

        public async Task<IHostProvider> GetProviderAsync(InputArguments input)
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
                    provider = _hostProviders
                        .SelectMany(x => x.Value)
                        .FirstOrDefault(x => StringComparer.OrdinalIgnoreCase.Equals(x.Id, providerId));

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
                    provider = _hostProviders
                        .SelectMany(x => x.Value)
                        .FirstOrDefault(x => x.SupportedAuthorityIds.Contains(authority, StringComparer.OrdinalIgnoreCase));

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

            var uri = input.GetRemoteUri();
            var queryResponse = new Lazy<Task<HttpResponseMessage>>(() =>
            {
                _context.Trace.WriteLine("Querying remote URL for host provider auto-detection.");
                return HttpClient.HeadAsync(uri);
            });

            async Task<IHostProvider> MatchProviderAsync(HostProviderPriority priority)
            {
                if (_hostProviders.TryGetValue(priority, out ICollection<IHostProvider> providers))
                {
                    _context.Trace.WriteLine($"Checking against {providers.Count} host providers registered with priority '{priority}'.");

                    // Try matching using the static Git input arguments first (cheap)
                    if (providers.TryGetFirst(x => x.IsSupported(input), out IHostProvider match))
                    {
                        return match;
                    }

                    HttpResponseMessage response = await queryResponse.Value;

                    // Try matching using the HTTP response from a query to the remote URL (expensive)
                    if (providers.TryGetFirst(x => x.IsSupported(response), out match))
                    {
                        return match;
                    }
                }

                return null;
            }

            // Match providers starting with the highest priority
            return await MatchProviderAsync(HostProviderPriority.High) ??
                   await MatchProviderAsync(HostProviderPriority.Normal) ??
                   await MatchProviderAsync(HostProviderPriority.Low) ??
                   throw new Exception("No host provider available to service this request.");
        }

        private HttpClient _httpClient;
        private HttpClient HttpClient => _httpClient ??= _context.HttpClientFactory.CreateClient();

        public void Dispose()
        {
            _httpClient?.Dispose();

            // Dispose of all registered providers to give them a chance to clean up and release any resources
            foreach (IHostProvider provider in _hostProviders.Values)
            {
                provider.Dispose();
            }
        }
    }
}
