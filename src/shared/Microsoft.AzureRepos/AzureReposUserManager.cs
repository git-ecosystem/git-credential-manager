// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Git.CredentialManager;

namespace Microsoft.AzureRepos
{
    /// <summary>
    /// Manages association of users and Git remotes for Azure Repos.
    /// </summary>
    public interface IAzureReposUserManager
    {
        /// <summary>
        /// Get the user for the given Azure DevOps organization or remote.
        /// </summary>
        /// <param name="remoteUri">Remote URI.</param>
        /// <returns>Identifier of user bound to the remote, or null if no binding exists.</returns>
        string GetUser(Uri remoteUri);

        /// <summary>
        /// Bind a user to an Azure DevOps organization or remote.
        /// </summary>
        /// <param name="remoteUri">Remote URI to bind.</param>
        /// <param name="userName">User identifier to bind.</param>
        void Bind(Uri remoteUri, string userName);

        /// <summary>
        /// Unbind an Azure DevOps organization or remote.
        /// </summary>
        /// <param name="remoteUri">Remote URL to unbind.</param>
        void Unbind(Uri remoteUri);

        /// <summary>
        /// Get all users that have been bound at the organization level.
        /// </summary>
        /// <returns>Users bound by organization.</returns>
        IDictionary<string, string> GetOrganizationBindings();

        /// <summary>
        /// Get all users that have been bound at the remote URL level.
        /// </summary>
        /// <returns>Users bound by remote URL.</returns>
        IDictionary<Uri, string> GetRemoteBindings();

        /// <summary>
        /// Bind a user to the given organization.
        /// </summary>
        /// <param name="orgName">Organization to bind the user to.</param>
        /// <param name="userName">User identifier to bind.</param>
        void BindOrganization(string orgName, string userName);

        /// <summary>
        /// Bind a user to the given remote URI.
        /// </summary>
        /// <param name="remoteUri">Remote URI to bind the user to.</param>
        /// <param name="userName">User identifier to bind.</param>
        void BindRemote(Uri remoteUri, string userName);

        /// <summary>
        /// Unbind the given remote URI.
        /// </summary>
        /// <param name="orgName">Organization to unbind.</param>
        void UnbindOrganization(string orgName);

        /// <summary>
        /// Unbind the given remote URI.
        /// </summary>
        /// <param name="remoteUri">Remote URI to unbind.</param>
        /// <param name="isExplicit">Mark the remote as explicitly unbound.</param>
        void UnbindRemote(Uri remoteUri, bool isExplicit = false);
    }

    public class AzureReposUserManager : IAzureReposUserManager
    {
        private readonly ITrace _trace;
        private readonly IScopedTransactionalStore _iniStore;

        public AzureReposUserManager(ICommandContext context)
            : this(context.Trace, new IniFileStore(context.FileSystem, new IniSerializer(), Path.Combine(
                context.FileSystem.UserDataDirectoryPath,
                AzureDevOpsConstants.AzReposDataDirectoryName,
                AzureDevOpsConstants.AzReposDataStoreName))) { }

        public AzureReposUserManager(ITrace trace, IScopedTransactionalStore iniStore)
        {
            EnsureArgument.NotNull(trace, nameof(trace));
            EnsureArgument.NotNull(iniStore, nameof(iniStore));

            _trace = trace;
            _iniStore = iniStore;
        }

        public string GetUser(Uri remoteUri)
        {
            EnsureArgument.AbsoluteUri(remoteUri, nameof(remoteUri));

            string orgName = UriHelpers.GetOrganizationName(remoteUri);
            string remoteKey = GetRemoteUserKey(remoteUri);
            string orgKey = GetOrgUserKey(orgName);

            /*
             * Always prefer the remote bindings over the organization bindings
             * If there is no remote binding this means 'inherit' the organization binding.
             * If the remote binding has a value of "null" this means do not inherit the organization binding.
             *
             */

            _iniStore.Reload();

            // Look for a binding to the specific remote
            _trace.WriteLine($"Looking up remote binding for '{remoteUri}'...");
            if (_iniStore.TryGetValue(remoteKey, out string remoteUser))
            {
                return remoteUser;
            }

            // Try to find an organization binding for this remote
            _trace.WriteLine($"Looking up organization binding for '{orgName}'...");
            if (_iniStore.TryGetValue(orgKey, out string orgUser))
            {
                return orgUser;
            }

            // No bound user
            return null;
        }


        public void Bind(Uri remoteUri, string userName)
        {
            EnsureArgument.AbsoluteUri(remoteUri, nameof(remoteUri));
            EnsureArgument.NotNullOrWhiteSpace(userName, nameof(userName));

            string orgName = UriHelpers.GetOrganizationName(remoteUri);
            string remoteKey = GetRemoteUserKey(remoteUri);
            string orgKey = GetOrgUserKey(orgName);

            /*
             *  State table change for binding user A
             *
             *     A = user being bound
             *     B = another user
             *     - = no binding
             *
             *   Current state   |    New state
             *   Org  |  Remote  |  Org  |  Remote
             * -------|----------|-------|----------
             *    -   |    -     |   A   |    -
             *    -   |    A     |   A   |    -
             *    -   |    B     |   A   |    -
             *    A   |    -     |   A   |    -
             *    A   |    A     |   A   |    -
             *    A   |    B     |   A   |    -
             *    B   |    -     |   B   |    A
             *    B   |    A     |   B   |    A
             *    B   |    B     |   B   |    A
             *
             */

            _iniStore.Reload();

            bool hasOrgBinding = _iniStore.TryGetValue(orgKey, out string orgUser);
            if (hasOrgBinding)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(userName, orgUser))
                {
                    // Org-level user is already correct; remove any remote-level binding
                    _trace.WriteLine($"Organization '{orgName}' is already bound to user '{orgUser}'.");
                    _trace.WriteLine($"Removing any explicit binding for remote '{remoteUri}'...");
                    _iniStore.Remove(remoteKey);
                }
                else
                {
                    // Org-level user is different; bind at the remote-level
                    _trace.WriteLine($"Organization '{orgName}' is bound to user '{orgUser}' which is different from '{userName}'.");
                    _trace.WriteLine($"Binding explicit remote '{remoteUri}' to user '{userName}'...");
                    _iniStore.SetValue(remoteKey, userName);
                }
            }
            else
            {
                // Bind at the org-level and clean up any remote-level binding
                _trace.WriteLine($"Binding organization '{orgName}' to user '{userName}'...");
                _iniStore.SetValue(orgKey, userName);
                _trace.WriteLine($"Removing any explicit binding for remote '{remoteUri}'...");
                _iniStore.Remove(remoteKey);
            }


            _iniStore.Commit();
        }

        public void Unbind(Uri remoteUri)
        {
            EnsureArgument.AbsoluteUri(remoteUri, nameof(remoteUri));

            string orgName = UriHelpers.GetOrganizationName(remoteUri);
            string orgKey = GetOrgUserKey(orgName);
            string remoteKey = GetRemoteUserKey(remoteUri);

            _trace.WriteLine($"Explicitly clearing sign-in state for specific remote '{remoteUri}'...");

            /*
             *  Sign-out state table change for signing-out the remote
             *
             *     U = bound user
             *     X = empty user (explicitly unbound/do not inherit)
             *     - = no binding
             *
             *  Note: Org = X is not a valid state
             *
             *   Current state   |    New state
             *   Org  |  Remote  |  Org  |  Remote
             * -------|----------|-------|----------
             *    -   |    -     |   -   |    -
             *    -   |    X     |   -   |    -
             *    -   |    U     |   -   |    -
             *    U   |    -     |   U   |    X
             *    U   |    X     |   U   |    X
             *    U   |    U     |   U   |    X
             *
             */

            _iniStore.Reload();

            bool hasOrgUser = _iniStore.TryGetValue(orgKey, out _);

            // If there is an org-level binding, set explicit remote binding
            if (hasOrgUser)
            {
                // Use an empty value to mean 'do not inherit' vs the absence of an entry meaning 'inherit'
                _iniStore.SetValue(remoteKey, null);
            }
            // If there is no org-level binding, just remove the remote binding
            else
            {
                _iniStore.Remove(remoteKey);
            }

            _iniStore.Commit();
        }

        public IDictionary<string, string> GetOrganizationBindings()
        {
            var dict = new Dictionary<string, string>();

            _iniStore.Reload();

            IEnumerable<string> orgNames = _iniStore.GetSectionScopes("org");
            foreach (string orgName in orgNames)
            {
                string orgUserKey = GetOrgUserKey(orgName);
                if (_iniStore.TryGetValue(orgUserKey, out string orgUser))
                {
                    dict[orgName] = orgUser;
                }
            }

            return dict;
        }

        public IDictionary<Uri, string> GetRemoteBindings()
        {
            var dict = new Dictionary<Uri, string>();

            _iniStore.Reload();

            IEnumerable<string> remotes = _iniStore.GetSectionScopes("remote");
            foreach (string remoteUrl in remotes)
            {
                if (!Uri.TryCreate(remoteUrl, UriKind.Absolute, out Uri remoteUri))
                {
                    continue;
                }

                string orgUserKey = GetRemoteUserKey(remoteUri);
                if (_iniStore.TryGetValue(orgUserKey, out string remoteUser))
                {
                    dict[remoteUri] = remoteUser;
                }
            }

            return dict;
        }

        public void BindOrganization(string orgName, string userName)
        {
            EnsureArgument.NotNullOrWhiteSpace(orgName, nameof(orgName));
            EnsureArgument.NotNullOrWhiteSpace(userName, nameof(userName));

            _iniStore.Reload();

            string key = GetOrgUserKey(orgName);

            _trace.WriteLine($"Binding user '{userName}' to organization '{orgName}'...");
            _iniStore.SetValue(key, userName);

            _iniStore.Commit();
        }

        public void BindRemote(Uri remoteUri, string userName)
        {
            EnsureArgument.AbsoluteUri(remoteUri, nameof(remoteUri));
            EnsureArgument.NotNullOrWhiteSpace(userName, nameof(userName));

            _iniStore.Reload();

            string key = GetRemoteUserKey(remoteUri);

            _trace.WriteLine($"Binding user '{userName}' to remote URL '{remoteUri}'...");
            _iniStore.SetValue(key, userName);

            _iniStore.Commit();
        }

        public void UnbindOrganization(string orgName)
        {
            EnsureArgument.NotNullOrWhiteSpace(orgName, nameof(orgName));

            _iniStore.Reload();

            string key = GetOrgUserKey(orgName);

            _trace.WriteLine($"Unbinding organization '{orgName}'...");
            _iniStore.Remove(key);

            _iniStore.Commit();
        }

        public void UnbindRemote(Uri remoteUri, bool isExplicit = false)
        {
            EnsureArgument.AbsoluteUri(remoteUri, nameof(remoteUri));

            _iniStore.Reload();

            string key = GetRemoteUserKey(remoteUri);

            if (isExplicit)
            {
                // Use the empty string value to signal an explicitly signed-out user
                _trace.WriteLine($"Explicitly unbinding remote URL '{remoteUri}'...");
                _iniStore.SetValue(key, string.Empty);
            }
            else
            {
                _trace.WriteLine($"Unbinding remote URL '{remoteUri}'...");
                _iniStore.Remove(key);
            }

            _iniStore.Commit();
        }

        private static string GetOrgUserKey(string orgName)
        {
            return $"org.{orgName}.user";
        }

        private static string GetRemoteUserKey(Uri uri)
        {
            return $"remote.{uri}.user";
        }
    }
}
