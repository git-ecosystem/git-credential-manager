using System;
using System.Collections.Generic;
using System.Linq;
using GitCredentialManager;

namespace Microsoft.ManagedApps
{
    /// <summary>
    /// Represents a single Microsoft Managed Apps deployment cloud environment (for example
    /// "prod", "preprod", "test", or a future sovereign cloud such as
    /// "gov"/"high"/"dod"/"mooncake").
    /// </summary>
    /// <remarks>
    /// A "cloud environment" here is distinct from a Power Platform/Dataverse "environment":
    /// many individual Power Platform environments (each with their own opaque per-environment
    /// Git host) belong to the same cloud environment. <see cref="HostSuffix"/> and the
    /// resource/scopes are tracked independently rather than derived from one another, since
    /// they are not guaranteed to follow the same naming pattern across cloud environments.
    /// </remarks>
    public sealed class ManagedAppsCloudEnvironment
    {
        public ManagedAppsCloudEnvironment(string name, string hostSuffix, string resourceAudience, IReadOnlyList<string> scopes)
        {
            EnsureArgument.NotNullOrWhiteSpace(name, nameof(name));

            Name = name;
            HostSuffix = hostSuffix;
            ResourceAudience = resourceAudience;
            Scopes = scopes;
        }

        /// <summary>
        /// Cloud environment identifier, e.g. "prod", "preprod", "test", "gov", "high", "dod",
        /// "mooncake".
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Host suffix used to recognize a Git remote as belonging to this cloud environment,
        /// e.g. ".environment.api.preprod.powerplatform.com".
        /// </summary>
        public string HostSuffix { get; }

        /// <summary>
        /// Resource URI used only for Managed Identity token requests (a single resource
        /// string, not a scopes array - see <see cref="GitCredentialManager.Authentication.IMicrosoftAuthentication.GetTokenForManagedIdentityAsync"/>).
        /// </summary>
        public string ResourceAudience { get; }

        /// <summary>
        /// Full OAuth scope URIs (plus "offline_access") requested for interactive user,
        /// service principal, and workload federation authentication.
        /// </summary>
        public IReadOnlyList<string> Scopes { get; }

        /// <summary>
        /// A cloud environment only participates in host matching once it has a complete
        /// definition (host suffix, resource, and scopes all present). This deliberately means
        /// an incomplete cloud environment (e.g. a known host suffix with resource/scopes not
        /// yet defined by the service) is never claimed-then-failed; it simply isn't matched,
        /// and the request safely falls through to the next provider (typically the generic
        /// OAuth provider).
        /// </summary>
        public bool IsComplete =>
            !string.IsNullOrWhiteSpace(HostSuffix) &&
            !string.IsNullOrWhiteSpace(ResourceAudience) &&
            Scopes != null && Scopes.Count > 0;

        #region Compiled-in defaults

        /// <summary>
        /// Compiled-in cloud environment defaults. Adding, extending, or completing a cloud
        /// environment should be limited to a single entry in this table - no other code in
        /// this project should ever need to branch on cloud environment name/identity.
        /// </summary>
        public static readonly IReadOnlyList<ManagedAppsCloudEnvironment> CompiledInDefaults = new[]
        {
            new ManagedAppsCloudEnvironment(
                name: "prod",
                hostSuffix: ".environment.api.powerplatform.com",
                resourceAudience: "https://api.powerplatform.com",
                scopes: new[]
                {
                    "https://api.powerplatform.com/.default",
                    "offline_access",
                }),

            // preprod/test: host suffix is already known, but resource/scopes have not yet
            // been defined by the service. Left as incomplete (null) entries on purpose -
            // TryMatch will not match these hosts until both fields are filled in here, or
            // completed via `credential.managedAppsCloudEnvironment.<name>.resource` / `.scopes`
            // configuration (field-level config merge - see ApplyConfigOverrides below).
            new ManagedAppsCloudEnvironment(
                name: "preprod",
                hostSuffix: ".environment.api.preprod.powerplatform.com",
                resourceAudience: null,
                scopes: null),

            new ManagedAppsCloudEnvironment(
                name: "test",
                hostSuffix: ".environment.api.test.powerplatform.com",
                resourceAudience: null,
                scopes: null),

            // Gov/High/DoD/Mooncake, and any future sovereign clouds: add one complete
            // entry each here once the service confirms host suffix + resource + scopes.
            // No other code changes should be required.
        };

        #endregion

        #region Matching

        /// <summary>
        /// Compute the effective cloud environment table: compiled-in defaults merged,
        /// field-by-field, with any `credential.managedAppsCloudEnvironment.&lt;name&gt;.*`
        /// Git configuration.
        /// </summary>
        public static IReadOnlyList<ManagedAppsCloudEnvironment> GetEffectiveCloudEnvironments(IGitConfiguration config)
        {
            EnsureArgument.NotNull(config, nameof(config));

            var byName = new Dictionary<string, CloudEnvironmentBuilder>(StringComparer.OrdinalIgnoreCase);

            foreach (ManagedAppsCloudEnvironment cloudEnvironment in CompiledInDefaults)
            {
                byName[cloudEnvironment.Name] = CloudEnvironmentBuilder.FromCloudEnvironment(cloudEnvironment);
            }

            ApplyConfigOverrides(config, byName);

            return byName.Values.Select(b => b.ToCloudEnvironment()).ToArray();
        }

        private static void ApplyConfigOverrides(IGitConfiguration config, IDictionary<string, CloudEnvironmentBuilder> byName)
        {
            void Apply(string property, Action<CloudEnvironmentBuilder, string> assign)
            {
                // Enumerating across all configuration levels relies on Git's own
                // system -> global -> local listing order, so a later (more specific) entry
                // for the same cloud environment/property correctly overrides an earlier one.
                config.Enumerate(GitConfigurationLevel.All, Constants.GitConfiguration.Credential.SectionName, property, entry =>
                {
                    if (GitConfigurationKeyComparer.TrySplit(entry.Key, out _, out string scope, out _) &&
                        scope != null &&
                        scope.StartsWith(ManagedAppsConstants.CloudEnvironmentConfigScopePrefix, StringComparison.Ordinal))
                    {
                        string cloudEnvironmentName = scope.Substring(ManagedAppsConstants.CloudEnvironmentConfigScopePrefix.Length);
                        if (!string.IsNullOrWhiteSpace(cloudEnvironmentName))
                        {
                            if (!byName.TryGetValue(cloudEnvironmentName, out CloudEnvironmentBuilder builder))
                            {
                                builder = new CloudEnvironmentBuilder(cloudEnvironmentName);
                                byName[cloudEnvironmentName] = builder;
                            }

                            assign(builder, entry.Value);
                        }
                    }

                    return true;
                });
            }

            Apply(ManagedAppsConstants.GitConfigCloudEnvironmentKeys.HostSuffix, (b, v) => b.HostSuffix = v);
            Apply(ManagedAppsConstants.GitConfigCloudEnvironmentKeys.Resource, (b, v) => b.ResourceAudience = v);
            Apply(ManagedAppsConstants.GitConfigCloudEnvironmentKeys.Scopes,
                (b, v) => b.Scopes = v?.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }

        private sealed class CloudEnvironmentBuilder
        {
            public CloudEnvironmentBuilder(string name) => Name = name;

            public string Name { get; }
            public string HostSuffix { get; set; }
            public string ResourceAudience { get; set; }
            public IReadOnlyList<string> Scopes { get; set; }

            public static CloudEnvironmentBuilder FromCloudEnvironment(ManagedAppsCloudEnvironment cloudEnvironment) =>
                new CloudEnvironmentBuilder(cloudEnvironment.Name)
                {
                    HostSuffix = cloudEnvironment.HostSuffix,
                    ResourceAudience = cloudEnvironment.ResourceAudience,
                    Scopes = cloudEnvironment.Scopes,
                };

            public ManagedAppsCloudEnvironment ToCloudEnvironment() =>
                new ManagedAppsCloudEnvironment(Name, HostSuffix, ResourceAudience, Scopes);
        }

        /// <summary>
        /// Try and find the (complete) cloud environment matching the given host, taking into
        /// account any configuration-based additions/completions. Incomplete cloud
        /// environments are never matched - see <see cref="IsComplete"/>.
        /// </summary>
        public static bool TryMatch(ITrace trace, IGitConfiguration config, string host, out ManagedAppsCloudEnvironment cloudEnvironment)
        {
            EnsureArgument.NotNull(trace, nameof(trace));

            return TryMatch(trace, GetEffectiveCloudEnvironments(config), host, out cloudEnvironment);
        }

        /// <summary>
        /// Try and find the (complete) cloud environment matching the given host within the
        /// supplied candidate table. Exposed separately from
        /// <see cref="GetEffectiveCloudEnvironments"/> for ease of unit testing pure matching
        /// behavior.
        /// </summary>
        internal static bool TryMatch(ITrace trace, IEnumerable<ManagedAppsCloudEnvironment> candidates, string host, out ManagedAppsCloudEnvironment cloudEnvironment)
        {
            cloudEnvironment = null;

            if (string.IsNullOrWhiteSpace(host))
            {
                return false;
            }

            ManagedAppsCloudEnvironment best = null;

            foreach (ManagedAppsCloudEnvironment candidate in candidates)
            {
                if (string.IsNullOrEmpty(candidate.HostSuffix) ||
                    !host.EndsWith(candidate.HostSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!candidate.IsComplete)
                {
                    trace?.WriteLine(
                        $"Host '{host}' matches Microsoft Managed Apps cloud environment '{candidate.Name}' by suffix, " +
                        "but that cloud environment is not yet fully configured (missing resource and/or scopes) - " +
                        "not claiming this request.");
                    continue;
                }

                // Prefer the longest (most specific) matching suffix.
                if (best is null || candidate.HostSuffix.Length > best.HostSuffix.Length)
                {
                    best = candidate;
                }
            }

            cloudEnvironment = best;
            return cloudEnvironment != null;
        }

        #endregion
    }
}
