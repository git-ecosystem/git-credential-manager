// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using Microsoft.Git.CredentialManager;

namespace Microsoft.AzureRepos
{
    /// <summary>
    /// Manages association of users and Azure Repos remotes.
    /// </summary>
    public interface IAzureReposUserManager
    {
        /// <summary>
        /// Get the identifier of a user signed-in to the given remote URI.
        /// </summary>
        /// <param name="remoteUri">Remote URI to query for a signed-in user for.</param>
        /// <returns>User identifier signed-in to the remote, or null if no user is signed-in.</returns>
        string GetUser(Uri remoteUri);

        /// <summary>
        /// Sign-in a user to the given remote URI.
        /// </summary>
        /// <param name="remoteUri">Remote URI to sign-in.</param>
        /// <param name="userName">Identifier of user to sign-in.</param>
        void SignIn(Uri remoteUri, string userName);

        /// <summary>
        /// Sign-out a user from the given remote URI.
        /// </summary>
        /// <param name="remoteUri">Remote URI to sign-out.</param>
        void SignOut(Uri remoteUri);
    }

    public class AzureReposUserManager : IAzureReposUserManager
    {
        private readonly ITrace _trace;
        private readonly ITransactionalValueStore<string, string> _store;

        public AzureReposUserManager(ITrace trace, ITransactionalValueStore<string, string> store)
        {
            EnsureArgument.NotNull(trace, nameof(trace));
            EnsureArgument.NotNull(store, nameof(store));

            _trace = trace;
            _store = store;
        }

        public string GetUser(Uri remoteUri)
        {
            EnsureArgument.NotNull(remoteUri, nameof(remoteUri));

            string orgName = UriHelpers.GetOrganizationName(remoteUri);
            string remoteKey = GetRemoteUserKey(remoteUri);
            string orgKey = GetOrgUserKey(orgName);

            /*
             * Always prefer the remote-level user over the organization-level user.
             * If the remote-level user has not been set this means 'inherit' the org-level user.
             * If the remote-level user has a value of "null" this means 'no user' and we do not inherit the org-level user.
             *
             */

            _store.Reload();

            // Look for a user who has been signed-in to the specific remote
            _trace.WriteLine($"Looking up signed-in user for specific remote '{remoteUri}'...");
            if (_store.TryGetValue(remoteKey, out string remoteUser))
            {
                return remoteUser;
            }

            // Try to find a user signed-in at the organization level for this remote
            _trace.WriteLine($"Looking up signed-in user for organization '{orgName}'...");
            if (_store.TryGetValue(orgKey, out string orgUser))
            {
                return orgUser;
            }

            // No signed-in user
            return null;
        }

        public void SignIn(Uri remoteUri, string userName)
        {
            EnsureArgument.NotNull(remoteUri, nameof(remoteUri));
            EnsureArgument.NotNull(userName, nameof(userName));

            string orgName = UriHelpers.GetOrganizationName(remoteUri);
            string remoteKey = GetRemoteUserKey(remoteUri);
            string orgKey = GetOrgUserKey(orgName);

            /*
             *  Sign-in state table change for signing-in user A
             *
             *     A = user being signed-in
             *     B = another user
             *     - = no user state
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

            _store.Reload();

            bool hasOrgUser = _store.TryGetValue(orgKey, out string orgUser);

            if (hasOrgUser)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(userName, orgUser))
                {
                    // Org-level user is already correct; remove any remote-level user state
                    _trace.WriteLine($"Organization '{orgName}' is already signed-in with user '{orgUser}'.");
                    _trace.WriteLine($"Removing any explicit sign-in state for remote '{remoteUri}'...");
                    _store.Remove(remoteKey);
                }
                else
                {
                    // Org-level user is different; sign in at the remote-level
                    _trace.WriteLine($"Organization '{orgName}' is signed-in with user '{orgUser}' which is different from '{userName}'.");
                    _trace.WriteLine($"Signing-in to explicit remote '{remoteUri}' with user '{userName}'...");
                    _store.SetValue(remoteKey, userName);
                }
            }
            else
            {
                // Sign-in at the org-level and clean up any remote-level user state
                _trace.WriteLine($"Signing-in to organization '{orgName}' with user '{userName}'...");
                _store.SetValue(orgKey, userName);
                _trace.WriteLine($"Removing any explicit sign-in state for remote '{remoteUri}'...");
                _store.Remove(remoteKey);
            }

            _store.Commit();
        }

        public void SignOut(Uri remoteUri)
        {
            EnsureArgument.NotNull(remoteUri, nameof(remoteUri));

            string orgName = UriHelpers.GetOrganizationName(remoteUri);
            string orgKey = GetOrgUserKey(orgName);
            string remoteKey = GetRemoteUserKey(remoteUri);

            _trace.WriteLine($"Explicitly clearing sign-in state for specific remote '{remoteUri}'...");

            /*
             *  Sign-out state table change for signing-out the remote
             *
             *     U = signed-in user
             *     X = empty user (explicit remote sign-out)
             *     - = no user state
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

            _store.Reload();

            bool hasOrgUser = _store.TryGetValue(orgKey, out _);

            // If there is an org-level user, set explicit remote sign-out state
            if (hasOrgUser)
            {
                // Use an empty value to mean 'no user' vs the absence of an entry meaning 'inherit user from org'
                _store.SetValue(remoteKey, null);
            }
            // If there is no org-level user signed-in, just remove the remote user entry
            else
            {
                _store.Remove(remoteKey);
            }

            _store.Commit();
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
