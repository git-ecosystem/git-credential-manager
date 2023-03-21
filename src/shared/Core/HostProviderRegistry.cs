using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace GitCredentialManager
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
            // Perform auto-detection network probe and remember the result
            //
            _context.Trace.WriteLine("Performing auto-detection of host provider.");

            var uri = input.GetRemoteUri();
            if (uri is null)
            {
                throw new Trace2Exception(_context.Trace2, "Unable to detect host provider without a remote URL");
            }

            // We can only probe HTTP(S) URLs - for SMTP, IMAP, etc we cannot do network probing
            bool canProbeUri = StringComparer.OrdinalIgnoreCase.Equals(uri.Scheme, "http") ||
                               StringComparer.OrdinalIgnoreCase.Equals(uri.Scheme, "https");

            var probeTimeout = TimeSpan.FromMilliseconds(_context.Settings.AutoDetectProviderTimeout);
            _context.Trace.WriteLine($"Auto-detect probe timeout is {probeTimeout.TotalSeconds} ms.");

            HttpResponseMessage probeResponse = null;

            async Task<IHostProvider> MatchProviderAsync(HostProviderPriority priority, bool probe)
            {
                if (_hostProviders.TryGetValue(priority, out ICollection<IHostProvider> providers))
                {
                    _context.Trace.WriteLine($"Checking against {providers.Count} host providers registered with priority '{priority}'.");

                    // Try matching using the static Git input arguments first (cheap)
                    if (providers.TryGetFirst(x => x.IsSupported(input), out IHostProvider match))
                    {
                        return match;
                    }

                    // Try matching using the HTTP response from a query to the remote URL (expensive).
                    // The user may have disabled this feature with a zero or negative timeout for performance reasons.
                    // We only probe the remote once and reuse the same response for all providers.
                    if (probe && probeTimeout.TotalMilliseconds > 0)
                    {
                        if (probeResponse is null)
                        {
                            _context.Trace.WriteLine("Querying remote URL for host provider auto-detection.");

                            using (HttpClient client = _context.HttpClientFactory.CreateClient())
                            {
                                client.Timeout = probeTimeout;

                                try
                                {
                                    probeResponse = await client.HeadAsync(uri);
                                }
                                catch (TaskCanceledException)
                                {
                                    _context.Streams.Error.WriteLine($"warning: auto-detection of host provider took too long (>{probeTimeout.TotalMilliseconds}ms)");
                                    _context.Streams.Error.WriteLine($"warning: see {Constants.HelpUrls.GcmAutoDetect} for more information.");
                                }
                                catch (Exception ex)
                                {
                                    // The auto detect probing failed for some other reason.
                                    // We don't particular care why, but we should not crash!
                                    _context.Streams.Error.WriteLine($"warning: failed to probe '{uri}' to detect provider");
                                    _context.Streams.Error.WriteLine($"warning: {ex.Message}");
                                    _context.Streams.Error.WriteLine($"warning: see {Constants.HelpUrls.GcmAutoDetect} for more information.");
                                }
                            }
                        }

                        if (providers.TryGetFirst(x => x.IsSupported(probeResponse), out match))
                        {
                            return match;
                        }
                    }
                }

                return null;
            }

            // Match providers starting with the highest priority
            IHostProvider match = await MatchProviderAsync(HostProviderPriority.High, canProbeUri) ??
                                  await MatchProviderAsync(HostProviderPriority.Normal, canProbeUri) ??
                                  await MatchProviderAsync(HostProviderPriority.Low, canProbeUri) ??
                                  throw new Exception("No host provider available to service this request.");

            // If we ended up making a network call then set the host provider explicitly
            // to avoid future calls!
            if (probeResponse != null)
            {
                IGitConfiguration gitConfig = _context.Git.GetConfiguration();
                var keyName = string.Format(CultureInfo.InvariantCulture, "{0}.{1}.{2}",
                    Constants.GitConfiguration.Credential.SectionName, uri.ToString().TrimEnd('/'),
                    Constants.GitConfiguration.Credential.Provider);

                try
                {
                    _context.Trace.WriteLine($"Remembering host provider for '{uri}' as '{match.Id}'...");
                    gitConfig.Set(GitConfigurationLevel.Global, keyName, match.Id);
                }
                catch (Exception ex)
                {
                    var message = "Failed to set host provider!";
                    _context.Trace.WriteLine(message);
                    _context.Trace.WriteException(ex);
                    _context.Trace2.WriteError(message);

                    _context.Streams.Error.WriteLine("warning: failed to remember result of host provider detection!");
                    _context.Streams.Error.WriteLine($"warning: try setting this manually: `git config --global {keyName} {match.Id}`");
                }
            }

            return match;
        }

        public void Dispose()
        {
            // Dispose of all registered providers to give them a chance to clean up and release any resources
            foreach (IHostProvider provider in _hostProviders.Values.SelectMany(x => x))
            {
                provider.Dispose();
            }
        }
    }
}
