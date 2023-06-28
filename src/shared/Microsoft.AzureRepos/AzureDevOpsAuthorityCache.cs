using System;
using System.Collections.Generic;
using System.Globalization;
using GitCredentialManager;

namespace Microsoft.AzureRepos
{
    public interface IAzureDevOpsAuthorityCache
    {
        /// <summary>
        /// Lookup the cached authority for the specified Azure DevOps organization.
        /// </summary>
        /// <param name="orgName">Azure DevOps organization name.</param>
        /// <returns>Authority for the organization, or null if not found.</returns>
        string GetAuthority(string orgName);

        /// <summary>
        /// Updates the cached authority for the specified Azure DevOps organization.
        /// </summary>
        /// <param name="orgName">Azure DevOps organization name.</param>
        /// <param name="authority">New authority value.</param>
        void UpdateAuthority(string orgName, string authority);

        /// <summary>
        /// Erase the cached authority for the specified Azure DevOps organization.
        /// </summary>
        /// <param name="orgName">Azure DevOps organization name.</param>
        void EraseAuthority(string orgName);

        /// <summary>
        /// Clear all cached authorities.
        /// </summary>
        void Clear();
    }

    public class AzureDevOpsAuthorityCache : IAzureDevOpsAuthorityCache
    {
        private readonly ITrace _trace;
        private readonly IGit _git;

        public AzureDevOpsAuthorityCache(ICommandContext context)
            : this(context.Trace, context.Git) { }

        public AzureDevOpsAuthorityCache(ITrace trace, IGit git)
        {
            EnsureArgument.NotNull(trace, nameof(trace));
            EnsureArgument.NotNull(git, nameof(git));

            _trace = trace;
            _git = git;
        }

        public string GetAuthority(string orgName)
        {
            EnsureArgument.NotNullOrWhiteSpace(orgName, nameof(orgName));

            _trace.WriteLine($"Looking up cached authority for organization '{orgName}'...");

            IGitConfiguration config = _git.GetConfiguration();

            if (config.TryGet(GitConfigurationLevel.Global, GitConfigurationType.Raw,
                GetAuthorityKey(orgName), out string authority))
            {
                return authority;
            }

            return null;
        }

        public void UpdateAuthority(string orgName, string authority)
        {
            EnsureArgument.NotNullOrWhiteSpace(orgName, nameof(orgName));

            _trace.WriteLine($"Updating cached authority for '{orgName}' to '{authority}'...");

            IGitConfiguration config = _git.GetConfiguration();
            config.Set(GitConfigurationLevel.Global, GetAuthorityKey(orgName), authority);
        }

        public void EraseAuthority(string orgName)
        {
            EnsureArgument.NotNullOrWhiteSpace(orgName, nameof(orgName));

            _trace.WriteLine($"Removing cached authority for '{orgName}'...");
            IGitConfiguration config = _git.GetConfiguration();
            config.Unset(GitConfigurationLevel.Global, GetAuthorityKey(orgName));
        }

        public void Clear()
        {
            _trace.WriteLine("Clearing all cached authorities...");

            IGitConfiguration config = _git.GetConfiguration();

            var orgKeys = new HashSet<string>(GitConfigurationKeyComparer.Instance);
            config.Enumerate(
                GitConfigurationLevel.Global,
                Constants.GitConfiguration.Credential.SectionName,
                AzureDevOpsConstants.GitConfiguration.Credential.AzureAuthority,
                entry =>
            {
                if (GitConfigurationKeyComparer.TrySplit(entry.Key, out _, out string scope, out _) &&
                    Uri.TryCreate(scope, UriKind.Absolute, out Uri orgUrn) &&
                    orgUrn.Scheme == AzureDevOpsConstants.UrnScheme)
                {
                    orgKeys.Add(entry.Key);
                }

                return true;
            });

            foreach (string orgKey in orgKeys)
            {
                config.Unset(GitConfigurationLevel.Global, orgKey);
            }
        }

        private static string GetAuthorityKey(string orgName)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}:{2}/{3}.{4}",
                Constants.GitConfiguration.Credential.SectionName,
                AzureDevOpsConstants.UrnScheme, AzureDevOpsConstants.UrnOrgPrefix, orgName,
                AzureDevOpsConstants.GitConfiguration.Credential.AzureAuthority);
        }
    }
}
