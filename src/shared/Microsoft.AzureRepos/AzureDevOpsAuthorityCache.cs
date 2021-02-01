// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;

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
        private readonly IScopedTransactionalStore _iniStore;

        public AzureDevOpsAuthorityCache(ICommandContext context)
            : this(context.Trace, new IniFileStore(context.FileSystem, new IniSerializer(), Path.Combine(
                context.FileSystem.UserDataDirectoryPath,
                AzureDevOpsConstants.AzReposDataDirectoryName,
                AzureDevOpsConstants.AzReposDataStoreName))) { }

        public AzureDevOpsAuthorityCache(ITrace trace, IScopedTransactionalStore iniStore)
        {
            EnsureArgument.NotNull(trace, nameof(trace));
            EnsureArgument.NotNull(iniStore, nameof(iniStore));

            _trace = trace;
            _iniStore = iniStore;
        }

        public string GetAuthority(string orgName)
        {
            EnsureArgument.NotNullOrWhiteSpace(orgName, nameof(orgName));

            _trace.WriteLine($"Looking up cached authority for organization '{orgName}'...");

            _iniStore.Reload();
            if (_iniStore.TryGetValue(GetAuthorityKey(orgName), out string authority))
            {
                return authority;
            }

            return null;
        }

        public void UpdateAuthority(string orgName, string authority)
        {
            EnsureArgument.NotNullOrWhiteSpace(orgName, nameof(orgName));

            _trace.WriteLine($"Updating cached authority for '{orgName}' to '{authority}'...");

            _iniStore.Reload();
            _iniStore.SetValue(GetAuthorityKey(orgName), authority);
            _iniStore.Commit();
        }

        public void EraseAuthority(string orgName)
        {
            EnsureArgument.NotNullOrWhiteSpace(orgName, nameof(orgName));

            _trace.WriteLine($"Removing cached authority for '{orgName}'...");
            _iniStore.Reload();
            _iniStore.Remove(GetAuthorityKey(orgName));
            _iniStore.Commit();
        }

        public void Clear()
        {
            _trace.WriteLine("Clearing all cached authorities...");
            _iniStore.Reload();
            IEnumerable<string> orgNames = _iniStore.GetSectionScopes("org");
            foreach (string orgName in orgNames)
            {
                _iniStore.Remove(GetAuthorityKey(orgName));
            }
            _iniStore.Commit();
        }

        private static string GetAuthorityKey(string orgName)
        {
            return $"org.{orgName}.authority";
        }
    }
}
