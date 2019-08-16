// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Microsoft.Git.CredentialManager;

namespace Microsoft.AzureRepos
{
    public interface IAzureReposAuthorityCache
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
    }

    public class AzureReposAuthorityCache : IAzureReposAuthorityCache
    {
        private readonly ITrace _trace;
        private readonly ITransactionalValueStore<string, string> _store;

        public AzureReposAuthorityCache(ITrace trace, ITransactionalValueStore<string, string> store)
        {
            EnsureArgument.NotNull(trace, nameof(trace));
            EnsureArgument.NotNull(store, nameof(store));

            _trace = trace;
            _store = store;
        }

        public string GetAuthority(string orgName)
        {
            EnsureArgument.NotNullOrWhiteSpace(orgName, nameof(orgName));

            _trace.WriteLine($"Looking up cached authority for organization '{orgName}'...");

            _store.Reload();
            if (_store.TryGetValue(GetAuthorityKey(orgName), out string authority))
            {
                return authority;
            }

            return null;
        }

        public void UpdateAuthority(string orgName, string authority)
        {
            EnsureArgument.NotNullOrWhiteSpace(orgName, nameof(orgName));

            _trace.WriteLine($"Updating cached authority for '{orgName}' to '{authority}'...");

            _store.Reload();
            _store.SetValue(GetAuthorityKey(orgName), authority);
            _store.Commit();
        }

        public void EraseAuthority(string orgName)
        {
            EnsureArgument.NotNullOrWhiteSpace(orgName, nameof(orgName));

            _trace.WriteLine($"Removing cached authority for '{orgName}'...");
            _store.Reload();
            _store.Remove(GetAuthorityKey(orgName));
            _store.Commit();
        }

        private static string GetAuthorityKey(string orgName)
        {
            return $"org.{orgName}.authority";
        }
    }
}
