// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using Microsoft.Git.CredentialManager.Tests.Objects;
using Xunit;

namespace Microsoft.AzureRepos.Tests
{
    public class AzureReposUserManagerTests
    {
        #region GetUser

        [Fact]
        public void AzureReposUserManager_GetUser_Null_ThrowException()
        {
            var dict  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var trace = new NullTrace();
            var store = new InMemoryValueStore<string, string>(dict);
            var cache = new AzureReposUserManager(trace, store);

            Assert.Throws<ArgumentNullException>(() => cache.GetUser(null));
        }

        [Fact]
        public void AzureReposUserManager_GetUser_NoUser_ReturnsNull()
        {
            var remote = new Uri("https://dev.azure.com/org/_git/repo");

            var trace = new NullTrace();
            var store = new InMemoryValueStore<string, string>(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            string authority = cache.GetUser(remote);

            Assert.Null(authority);
        }

        [Fact]
        public void AzureReposUserManager_GetUser_OrgUser_ReturnsOrgUser()
        {
            var remote = new Uri("https://dev.azure.com/org/_git/repo");
            string orgKey = GetOrgUserKey("org");
            string orgUser = "john.doe";

            var dict  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [orgKey] = orgUser
            };

            var trace = new NullTrace();
            var store = new InMemoryValueStore<string, string>(dict);
            var cache = new AzureReposUserManager(trace, store);

            string actualUser = cache.GetUser(remote);

            Assert.Equal(orgUser, actualUser);
        }

        [Fact]
        public void AzureReposUserManager_GetUser_RemoteUser_ReturnsRemoteUser()
        {
            var remote = new Uri("https://dev.azure.com/org/_git/repo");
            string remoteKey = GetRemoteUserKey(remote);
            string remoteUser = "john.doe";

            var dict  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [remoteKey] = remoteUser
            };

            var trace = new NullTrace();
            var store = new InMemoryValueStore<string, string>(dict);
            var cache = new AzureReposUserManager(trace, store);

            string actualUser = cache.GetUser(remote);

            Assert.Equal(remoteUser, actualUser);
        }

        [Fact]
        public void AzureReposUserManager_GetUser_OrgAndRemoteUser_ReturnsRemoteUser()
        {
            var remote = new Uri("https://dev.azure.com/org/_git/repo");
            string orgKey = GetOrgUserKey("org");
            string orgUser = "john.doe";
            string remoteKey = GetRemoteUserKey(remote);
            string remoteUser = "joe.bloggs";

            var dict  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [orgKey] = orgUser,
                [remoteKey] = remoteUser,
            };

            var trace = new NullTrace();
            var store = new InMemoryValueStore<string, string>(dict);
            var cache = new AzureReposUserManager(trace, store);

            string actualUser = cache.GetUser(remote);

            Assert.Equal(remoteUser, actualUser);
        }

        [Fact]
        public void AzureReposUserManager_GetUser_OrgAndSignedOutRemoteUser_ReturnsNull()
        {
            var remote = new Uri("https://dev.azure.com/org/_git/repo");
            string orgKey = GetOrgUserKey("org");
            string orgUser = "john.doe";
            string remoteKey = GetRemoteUserKey(remote);

            var dict  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [orgKey] = orgUser,
                [remoteKey] = null,
            };

            var trace = new NullTrace();
            var store = new InMemoryValueStore<string, string>(dict);
            var cache = new AzureReposUserManager(trace, store);

            string actualUser = cache.GetUser(remote);

            Assert.Null(actualUser);
        }

        [Fact]
        public void AzureReposUserManager_GetUser_OrgUser_PersistedStoreChanged_ReturnsPersistedOrgUser()
        {
            var remote = new Uri("https://dev.azure.com/org/_git/repo");
            string orgKey = GetOrgUserKey("org");
            string orgUser = "john.doe";
            const string oldOrgUser = "joe.bloggs";

            var dict  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [orgKey] = oldOrgUser
            };

            var trace = new NullTrace();
            var store = new InMemoryValueStore<string, string>(dict);
            var cache = new AzureReposUserManager(trace, store);

            // Update persisted store after creation of the authority cache
            store.PersistedStore[orgKey] = orgUser;
            // The in-memory value should be stale
            Assert.Equal(oldOrgUser, store.MemoryStore[orgKey]);

            string actualUser = cache.GetUser(remote);

            // Should have reloaded from the persisted store
            Assert.Equal(orgUser, actualUser);
        }

        #endregion

        #region SignIn

        [Fact]
        public void AzureReposUserManager_SignIn_Null_ThrowException()
        {
            var remote = new Uri("https://dev.azure.com/org/_git/repo");

            var dict  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var trace = new NullTrace();
            var store = new InMemoryValueStore<string, string>(dict);
            var cache = new AzureReposUserManager(trace, store);

            Assert.Throws<ArgumentNullException>(() => cache.SignIn(null, "user"));
            Assert.Throws<ArgumentNullException>(() => cache.SignIn(remote, null));
        }

        [Fact]
        public void AzureReposUserManager_SignIn_NoOrgUser_SignsInOrg()
        {
            var remote = new Uri("https://dev.azure.com/org/_git/repo");
            string orgKey = GetOrgUserKey("org");
            const string user = "john.doe";

            var trace = new NullTrace();
            var store = new InMemoryValueStore<string, string>(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            cache.SignIn(remote, user);

            Assert.Single(store.PersistedStore);
            Assert.True(store.PersistedStore.TryGetValue(orgKey, out string actualUser));
            Assert.Equal(user, actualUser);
        }

        [Fact]
        public void AzureReposUserManager_SignIn_SameOrgUser_DoesNothing()
        {
            var remote = new Uri("https://dev.azure.com/org/_git/repo");
            string orgKey = GetOrgUserKey("org");
            const string user = "john.doe";

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [orgKey] = user
            };

            var trace = new NullTrace();
            var store = new InMemoryValueStore<string, string>(dict);
            var cache = new AzureReposUserManager(trace, store);

            cache.SignIn(remote, user);

            Assert.Single(store.PersistedStore);
            Assert.True(store.PersistedStore.TryGetValue(orgKey, out string actualUser));
            Assert.Equal(user, actualUser);
        }

        [Fact]
        public void AzureReposUserManager_SignIn_DifferentOrgUser_SignsInRemote()
        {
            var remote = new Uri("https://dev.azure.com/org/_git/repo");
            string orgKey = GetOrgUserKey("org");
            string remoteKey = GetRemoteUserKey(remote);
            const string user = "john.doe";
            const string otherUser = "joe.bloggs";

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [orgKey] = otherUser
            };

            var trace = new NullTrace();
            var store = new InMemoryValueStore<string, string>(dict);
            var cache = new AzureReposUserManager(trace, store);

            cache.SignIn(remote, user);

            Assert.Equal(2, store.PersistedStore.Count);
            Assert.True(store.PersistedStore.TryGetValue(orgKey, out string actualOrgUser));
            Assert.Equal(otherUser, actualOrgUser);
            Assert.True(store.PersistedStore.TryGetValue(remoteKey, out string actualRemoteUser));
            Assert.Equal(user, actualRemoteUser);
        }

        [Fact]
        public void AzureReposUserManager_SignIn_OrgAndRemoteUser_SignsInRemote()
        {
            var remote = new Uri("https://dev.azure.com/org/_git/repo");
            string orgKey = GetOrgUserKey("org");
            string remoteKey = GetRemoteUserKey(remote);
            const string user = "john.doe";
            const string otherUser1 = "joe.bloggs";
            const string otherUser2 = "jane.doe";

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [orgKey] = otherUser1,
                [remoteKey] = otherUser2
            };

            var trace = new NullTrace();
            var store = new InMemoryValueStore<string, string>(dict);
            var cache = new AzureReposUserManager(trace, store);

            cache.SignIn(remote, user);

            Assert.Equal(2, store.PersistedStore.Count);
            Assert.True(store.PersistedStore.TryGetValue(orgKey, out string actualOrgUser));
            Assert.Equal(otherUser1, actualOrgUser);
            Assert.True(store.PersistedStore.TryGetValue(remoteKey, out string actualRemoteUser));
            Assert.Equal(user, actualRemoteUser);
        }

        [Fact]
        public void AzureReposUserManager_SignIn_OrgAndSignedOutRemoteUser_SignsInRemote()
        {
            var remote = new Uri("https://dev.azure.com/org/_git/repo");
            string orgKey = GetOrgUserKey("org");
            string remoteKey = GetRemoteUserKey(remote);
            const string user = "john.doe";
            const string otherUser = "joe.bloggs";

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [orgKey] = otherUser,
                [remoteKey] = null
            };

            var trace = new NullTrace();
            var store = new InMemoryValueStore<string, string>(dict);
            var cache = new AzureReposUserManager(trace, store);

            cache.SignIn(remote, user);

            Assert.Equal(2, store.PersistedStore.Count);
            Assert.True(store.PersistedStore.TryGetValue(orgKey, out string actualOrgUser));
            Assert.Equal(otherUser, actualOrgUser);
            Assert.True(store.PersistedStore.TryGetValue(remoteKey, out string actualRemoteUser));
            Assert.Equal(user, actualRemoteUser);
        }

        [Fact]
        public void AzureReposUserManager_SignIn_SameRemoteUserOnly_SignsInOrgRemovesRemote()
        {
            var remote = new Uri("https://dev.azure.com/org/_git/repo");
            string orgKey = GetOrgUserKey("org");
            string remoteKey = GetRemoteUserKey(remote);
            const string user = "john.doe";

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [remoteKey] = user
            };

            var trace = new NullTrace();
            var store = new InMemoryValueStore<string, string>(dict);
            var cache = new AzureReposUserManager(trace, store);

            cache.SignIn(remote, user);

            Assert.Single(store.PersistedStore);
            Assert.True(store.PersistedStore.TryGetValue(orgKey, out string actualOrgUser));
            Assert.Equal(user, actualOrgUser);
        }

        [Fact]
        public void AzureReposUserManager_SignIn_DifferentRemoteUserOnly_SignsInOrgRemovesRemote()
        {
            var remote = new Uri("https://dev.azure.com/org/_git/repo");
            string orgKey = GetOrgUserKey("org");
            string remoteKey = GetRemoteUserKey(remote);
            const string user = "john.doe";
            const string otherUser = "joe.bloggs";

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [remoteKey] = otherUser
            };

            var trace = new NullTrace();
            var store = new InMemoryValueStore<string, string>(dict);
            var cache = new AzureReposUserManager(trace, store);

            cache.SignIn(remote, user);

            Assert.Single(store.PersistedStore);
            Assert.True(store.PersistedStore.TryGetValue(orgKey, out string actualOrgUser));
            Assert.Equal(user, actualOrgUser);
        }

        // TODO: persisted change test

        #endregion

        #region SignOut

        [Fact]
        public void AzureReposUserManager_SignOut_Null_ThrowException()
        {
            var dict  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var trace = new NullTrace();
            var store = new InMemoryValueStore<string, string>(dict);
            var cache = new AzureReposUserManager(trace, store);

            Assert.Throws<ArgumentNullException>(() => cache.SignOut(null));
        }

        [Fact]
        public void AzureReposUserManager_SignOut_NoOrgUserNoRemoteUser_DoesNothing()
        {
            var remote = new Uri("https://dev.azure.com/org/_git/repo");
            string remoteKey = GetRemoteUserKey(remote);

            var trace = new NullTrace();
            var store = new InMemoryValueStore<string, string>(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            cache.SignOut(remote);

            Assert.Empty(store.PersistedStore);
        }

        [Fact]
        public void AzureReposUserManager_SignOut_OrgUser_RemoteExplicitlySignedOut()
        {
            var remote = new Uri("https://dev.azure.com/org/_git/repo");
            string orgKey = GetOrgUserKey("org");
            string remoteKey = GetRemoteUserKey(remote);
            const string user = "john.doe";

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [orgKey] = user
            };

            var trace = new NullTrace();
            var store = new InMemoryValueStore<string, string>(dict);
            var cache = new AzureReposUserManager(trace, store);

            cache.SignOut(remote);

            Assert.Equal(2, store.PersistedStore.Count);
            Assert.True(store.PersistedStore.TryGetValue(orgKey, out string actualOrgUser));
            Assert.Equal(user, actualOrgUser);
            Assert.True(store.PersistedStore.TryGetValue(remoteKey, out string actualRemoteUser));
            Assert.Null(actualRemoteUser);
        }

        [Fact]
        public void AzureReposUserManager_SignOut_OrgAndRemoteUser_RemoteExplicitlySignedOut()
        {
            var remote = new Uri("https://dev.azure.com/org/_git/repo");
            string orgKey = GetOrgUserKey("org");
            string remoteKey = GetRemoteUserKey(remote);
            const string user = "john.doe";
            const string otherUser = "joe.bloggs";

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [orgKey] = user,
                [remoteKey] = otherUser,
            };

            var trace = new NullTrace();
            var store = new InMemoryValueStore<string, string>(dict);
            var cache = new AzureReposUserManager(trace, store);

            cache.SignOut(remote);

            Assert.Equal(2, store.PersistedStore.Count);
            Assert.True(store.PersistedStore.TryGetValue(orgKey, out string actualOrgUser));
            Assert.Equal(user, actualOrgUser);
            Assert.True(store.PersistedStore.TryGetValue(remoteKey, out string actualRemoteUser));
            Assert.Null(actualRemoteUser);
        }

        [Fact]
        public void AzureReposUserManager_SignOut_RemoteUser_RemovesRemoteUser()
        {
            var remote = new Uri("https://dev.azure.com/org/_git/repo");
            string remoteKey = GetRemoteUserKey(remote);
            const string user = "john.doe";

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [remoteKey] = user,
            };

            var trace = new NullTrace();
            var store = new InMemoryValueStore<string, string>(dict);
            var cache = new AzureReposUserManager(trace, store);

            cache.SignOut(remote);

            Assert.Empty(store.PersistedStore);
        }

        [Fact]
        public void AzureReposUserManager_SignOut_OrgAndSignedOutRemoteUser_DoesNothing()
        {
            var remote = new Uri("https://dev.azure.com/org/_git/repo");
            string orgKey = GetOrgUserKey("org");
            string remoteKey = GetRemoteUserKey(remote);
            const string user = "john.doe";

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [orgKey] = user,
                [remoteKey] = null
            };

            var trace = new NullTrace();
            var store = new InMemoryValueStore<string, string>(dict);
            var cache = new AzureReposUserManager(trace, store);

            cache.SignOut(remote);

            Assert.Equal(2, store.PersistedStore.Count);
            Assert.True(store.PersistedStore.TryGetValue(orgKey, out string actualOrgUser));
            Assert.Equal(user, actualOrgUser);
            Assert.True(store.PersistedStore.TryGetValue(remoteKey, out string actualRemoteUser));
            Assert.Null(actualRemoteUser);
        }

        // TODO: persisted change test

        #endregion

        #region Helpers

        private static string GetOrgUserKey(string orgName)
        {
            return $"org.{orgName}.user";
        }

        private static string GetRemoteUserKey(Uri uri)
        {
            return $"remote.{uri}.user";
        }

        #endregion
    }
}
