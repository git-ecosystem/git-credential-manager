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
        #region BindOrganization

        [Fact]
        public void AzureReposUserManager_BindOrganization_NullOrganization_ThrowException()
        {
            var dict  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var trace = new NullTrace();
            var store = new InMemoryIniStore(dict);
            var cache = new AzureReposUserManager(trace, store);

            Assert.Throws<ArgumentNullException>(() => cache.BindOrganization(null, "user"));
        }


        [Fact]
        public void AzureReposUserManager_BindOrganization_NoUser_SetsOrgKey()
        {
            const string expectedUser = "user1";
            const string orgName = "org";
            var remote = new Uri("https://dev.azure.com/org/_git/repo");

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            cache.BindOrganization(orgName, expectedUser);

            Assert.True(store.PersistedStore.TryGetValue(GetOrgUserKey(orgName), out string actualUser));
            Assert.Equal(expectedUser, actualUser);
        }

        [Fact]
        public void AzureReposUserManager_BindOrganization_ExistingUser_SetsOrgKey()
        {
            const string expectedUser = "user1";
            const string orgName = "org";
            var remote = new Uri("https://dev.azure.com/org/_git/repo");

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            store.PersistedStore[GetOrgUserKey(orgName)] = "org-user";
            store.PersistedStore[GetRemoteUserKey(remote)] = "remote-user";

            cache.BindOrganization(orgName, expectedUser);

            Assert.True(store.PersistedStore.TryGetValue(GetOrgUserKey(orgName), out string actualUser));
            Assert.Equal(expectedUser, actualUser);
        }

        #endregion

        #region BindRemote

        [Fact]
        public void AzureReposUserManager_BindRemote_NullUri_ThrowException()
        {
            var dict  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var trace = new NullTrace();
            var store = new InMemoryIniStore(dict);
            var cache = new AzureReposUserManager(trace, store);

            Assert.Throws<ArgumentNullException>(() => cache.BindRemote(null, "user"));
        }

        [Fact]
        public void AzureReposUserManager_BindRemote_NoUser_SetsRemoteKey()
        {
            const string expectedUser = "user1";
            var remote = new Uri("https://dev.azure.com/org/_git/repo");

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            cache.BindRemote(remote, expectedUser);

            Assert.True(store.PersistedStore.TryGetValue(GetRemoteUserKey(remote), out string actualUser));
            Assert.Equal(expectedUser, actualUser);
        }

        [Fact]
        public void AzureReposUserManager_BindRemote_ExistingUser_SetsRemoteKey()
        {
            const string expectedUser = "user1";
            const string orgName = "org";
            var remote = new Uri("https://dev.azure.com/org/_git/repo");

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            store.PersistedStore[GetOrgUserKey(orgName)] = "org-user";
            store.PersistedStore[GetRemoteUserKey(remote)] = "remote-user";

            cache.BindRemote(remote, expectedUser);

            Assert.True(store.PersistedStore.TryGetValue(GetRemoteUserKey(remote), out string actualUser));
            Assert.Equal(expectedUser, actualUser);
        }

        #endregion

        #region UnbindOrganization

        [Fact]
        public void AzureReposUserManager_UnbindOrganization_NullOrganization_ThrowException()
        {
            var dict  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var trace = new NullTrace();
            var store = new InMemoryIniStore(dict);
            var cache = new AzureReposUserManager(trace, store);

            Assert.Throws<ArgumentNullException>(() => cache.UnbindOrganization(null));
        }
        [Fact]
        public void AzureReposUserManager_UnbindOrganization_NoUser_DoesNothing()
        {
            const string orgName = "org";

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            cache.UnbindOrganization(orgName);

            Assert.False(store.PersistedStore.TryGetValue(GetOrgUserKey(orgName), out string _));
        }

        [Fact]
        public void AzureReposUserManager_UnbindOrganization_ExistingUser_RemovesOrgKey()
        {
            const string orgName = "org";
            var remote = new Uri("https://dev.azure.com/org/_git/repo");

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            store.PersistedStore[GetOrgUserKey(orgName)] = "org-user";
            store.PersistedStore[GetRemoteUserKey(remote)] = "remote-user";

            cache.UnbindOrganization(orgName);

            Assert.False(store.PersistedStore.TryGetValue(GetOrgUserKey(orgName), out string actualUser));
        }

        #endregion

        #region UnbindRemote

        [Fact]
        public void AzureReposUserManager_UnbindRemote_NullUri_ThrowException()
        {
            var dict  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var trace = new NullTrace();
            var store = new InMemoryIniStore(dict);
            var cache = new AzureReposUserManager(trace, store);

            Assert.Throws<ArgumentNullException>(() => cache.UnbindRemote(null));
        }


        [Fact]
        public void AzureReposUserManager_UnbindRemote_NoUser_DoesNothing()
        {
            var remote = new Uri("https://dev.azure.com/org/_git/repo");

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            cache.UnbindRemote(remote);

            Assert.False(store.PersistedStore.TryGetValue(GetRemoteUserKey(remote), out string _));
        }

        [Fact]
        public void AzureReposUserManager_UnbindRemote_ExistingUser_RemovesRemoteKey()
        {
            const string orgName = "org";
            var remote = new Uri("https://dev.azure.com/org/_git/repo");

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            store.PersistedStore[GetOrgUserKey(orgName)] = "org-user";
            store.PersistedStore[GetRemoteUserKey(remote)] = "remote-user";

            cache.UnbindRemote(remote);

            Assert.False(store.PersistedStore.TryGetValue(GetRemoteUserKey(remote), out string actualUser));
        }

        [Fact]
        public void AzureReposUserManager_UnbindRemote_Explicit_ExistingUser_SetsRemoteKeyEmptyString()
        {
            const string orgName = "org";
            var remote = new Uri("https://dev.azure.com/org/_git/repo");

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            store.PersistedStore[GetOrgUserKey(orgName)] = "org-user";
            store.PersistedStore[GetRemoteUserKey(remote)] = "remote-user";

            cache.UnbindRemote(remote, isExplicit: true);

            Assert.True(store.PersistedStore.TryGetValue(GetRemoteUserKey(remote), out string actualUser));
            Assert.Equal(string.Empty, actualUser);
        }

        #endregion

        #region GetUser

        [Fact]
        public void AzureReposUserManager_GetUser_Null_ThrowException()
        {
            var dict  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var trace = new NullTrace();
            var store = new InMemoryIniStore(dict);
            var cache = new AzureReposUserManager(trace, store);

            Assert.Throws<ArgumentNullException>(() => cache.GetUser(null));
        }

        [Fact]
        public void AzureReposUserManager_GetUser_NoUser_ReturnsNull()
        {
            var remote = new Uri("https://dev.azure.com/org/_git/repo");

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
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
            var store = new InMemoryIniStore(dict);
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
            var store = new InMemoryIniStore(dict);
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
            var store = new InMemoryIniStore(dict);
            var cache = new AzureReposUserManager(trace, store);

            string actualUser = cache.GetUser(remote);

            Assert.Equal(remoteUser, actualUser);
        }

        [Fact]
        public void AzureReposUserManager_GetUser_OrgAndNoInheritRemoteUser_ReturnsNull()
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
            var store = new InMemoryIniStore(dict);
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
            var store = new InMemoryIniStore(dict);
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

        #region Bind

        [Fact]
        public void AzureReposUserManager_Bind_Null_ThrowException()
        {
            var remote = new Uri("https://dev.azure.com/org/_git/repo");

            var dict  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var trace = new NullTrace();
            var store = new InMemoryIniStore(dict);
            var cache = new AzureReposUserManager(trace, store);

            Assert.Throws<ArgumentNullException>(() => cache.Bind(null, "user"));
            Assert.Throws<ArgumentNullException>(() => cache.Bind(remote, null));
        }

        [Fact]
        public void AzureReposUserManager_Bind_NoOrgUser_SignsInOrg()
        {
            var remote = new Uri("https://dev.azure.com/org/_git/repo");
            string orgKey = GetOrgUserKey("org");
            const string user = "john.doe";

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            cache.Bind(remote, user);

            Assert.Single(store.PersistedStore);
            Assert.True(store.PersistedStore.TryGetValue(orgKey, out string actualUser));
            Assert.Equal(user, actualUser);
        }

        [Fact]
        public void AzureReposUserManager_Bind_SameOrgUser_DoesNothing()
        {
            var remote = new Uri("https://dev.azure.com/org/_git/repo");
            string orgKey = GetOrgUserKey("org");
            const string user = "john.doe";

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [orgKey] = user
            };

            var trace = new NullTrace();
            var store = new InMemoryIniStore(dict);
            var cache = new AzureReposUserManager(trace, store);

            cache.Bind(remote, user);

            Assert.Single(store.PersistedStore);
            Assert.True(store.PersistedStore.TryGetValue(orgKey, out string actualUser));
            Assert.Equal(user, actualUser);
        }

        [Fact]
        public void AzureReposUserManager_Bind_DifferentOrgUser_SignsInRemote()
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
            var store = new InMemoryIniStore(dict);
            var cache = new AzureReposUserManager(trace, store);

            cache.Bind(remote, user);

            Assert.Equal(2, store.PersistedStore.Count);
            Assert.True(store.PersistedStore.TryGetValue(orgKey, out string actualOrgUser));
            Assert.Equal(otherUser, actualOrgUser);
            Assert.True(store.PersistedStore.TryGetValue(remoteKey, out string actualRemoteUser));
            Assert.Equal(user, actualRemoteUser);
        }

        [Fact]
        public void AzureReposUserManager_Bind_OrgAndRemoteUser_SignsInRemote()
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
            var store = new InMemoryIniStore(dict);
            var cache = new AzureReposUserManager(trace, store);

            cache.Bind(remote, user);

            Assert.Equal(2, store.PersistedStore.Count);
            Assert.True(store.PersistedStore.TryGetValue(orgKey, out string actualOrgUser));
            Assert.Equal(otherUser1, actualOrgUser);
            Assert.True(store.PersistedStore.TryGetValue(remoteKey, out string actualRemoteUser));
            Assert.Equal(user, actualRemoteUser);
        }

        [Fact]
        public void AzureReposUserManager_Bind_OrgAndSignedOutRemoteUser_SignsInRemote()
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
            var store = new InMemoryIniStore(dict);
            var cache = new AzureReposUserManager(trace, store);

            cache.Bind(remote, user);

            Assert.Equal(2, store.PersistedStore.Count);
            Assert.True(store.PersistedStore.TryGetValue(orgKey, out string actualOrgUser));
            Assert.Equal(otherUser, actualOrgUser);
            Assert.True(store.PersistedStore.TryGetValue(remoteKey, out string actualRemoteUser));
            Assert.Equal(user, actualRemoteUser);
        }

        [Fact]
        public void AzureReposUserManager_Bind_SameRemoteUserOnly_SignsInOrgRemovesRemote()
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
            var store = new InMemoryIniStore(dict);
            var cache = new AzureReposUserManager(trace, store);

            cache.Bind(remote, user);

            Assert.Single(store.PersistedStore);
            Assert.True(store.PersistedStore.TryGetValue(orgKey, out string actualOrgUser));
            Assert.Equal(user, actualOrgUser);
        }

        [Fact]
        public void AzureReposUserManager_Bind_DifferentRemoteUserOnly_SignsInOrgRemovesRemote()
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
            var store = new InMemoryIniStore(dict);
            var cache = new AzureReposUserManager(trace, store);

            cache.Bind(remote, user);

            Assert.Single(store.PersistedStore);
            Assert.True(store.PersistedStore.TryGetValue(orgKey, out string actualOrgUser));
            Assert.Equal(user, actualOrgUser);
        }

        // TODO: persisted change test

        #endregion

        #region Unbind

        [Fact]
        public void AzureReposUserManager_Unbind_Null_ThrowException()
        {
            var dict  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var trace = new NullTrace();
            var store = new InMemoryIniStore(dict);
            var cache = new AzureReposUserManager(trace, store);

            Assert.Throws<ArgumentNullException>(() => cache.Unbind(null));
        }

        [Fact]
        public void AzureReposUserManager_Unbind_NoOrgUserNoRemoteUser_DoesNothing()
        {
            var remote = new Uri("https://dev.azure.com/org/_git/repo");
            string remoteKey = GetRemoteUserKey(remote);

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            cache.Unbind(remote);

            Assert.Empty(store.PersistedStore);
        }

        [Fact]
        public void AzureReposUserManager_Unbind_OrgUser_RemoteExplicitlySignedOut()
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
            var store = new InMemoryIniStore(dict);
            var cache = new AzureReposUserManager(trace, store);

            cache.Unbind(remote);

            Assert.Equal(2, store.PersistedStore.Count);
            Assert.True(store.PersistedStore.TryGetValue(orgKey, out string actualOrgUser));
            Assert.Equal(user, actualOrgUser);
            Assert.True(store.PersistedStore.TryGetValue(remoteKey, out string actualRemoteUser));
            Assert.Null(actualRemoteUser);
        }

        [Fact]
        public void AzureReposUserManager_Unbind_OrgAndRemoteUser_RemoteExplicitlySignedOut()
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
            var store = new InMemoryIniStore(dict);
            var cache = new AzureReposUserManager(trace, store);

            cache.Unbind(remote);

            Assert.Equal(2, store.PersistedStore.Count);
            Assert.True(store.PersistedStore.TryGetValue(orgKey, out string actualOrgUser));
            Assert.Equal(user, actualOrgUser);
            Assert.True(store.PersistedStore.TryGetValue(remoteKey, out string actualRemoteUser));
            Assert.Null(actualRemoteUser);
        }

        [Fact]
        public void AzureReposUserManager_Unbind_RemoteUser_RemovesRemoteUser()
        {
            var remote = new Uri("https://dev.azure.com/org/_git/repo");
            string remoteKey = GetRemoteUserKey(remote);
            const string user = "john.doe";

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [remoteKey] = user,
            };

            var trace = new NullTrace();
            var store = new InMemoryIniStore(dict);
            var cache = new AzureReposUserManager(trace, store);

            cache.Unbind(remote);

            Assert.Empty(store.PersistedStore);
        }

        [Fact]
        public void AzureReposUserManager_Unbind_OrgAndSignedOutRemoteUser_DoesNothing()
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
            var store = new InMemoryIniStore(dict);
            var cache = new AzureReposUserManager(trace, store);

            cache.Unbind(remote);

            Assert.Equal(2, store.PersistedStore.Count);
            Assert.True(store.PersistedStore.TryGetValue(orgKey, out string actualOrgUser));
            Assert.Equal(user, actualOrgUser);
            Assert.True(store.PersistedStore.TryGetValue(remoteKey, out string actualRemoteUser));
            Assert.Null(actualRemoteUser);
        }

        #endregion

        #region GetBindings

        [Fact]
        public void AzureReposUserManager_GetRemoteBindings_NoUsers_ReturnsEmpty()
        {
            var expected = new Dictionary<Uri, string>();

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            IDictionary<Uri, string> actual = cache.GetRemoteBindings();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void AzureReposUserManager_GetOrganizationBindings_NoUsers_ReturnsEmpty()
        {
            var expected = new Dictionary<string, string>();

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            IDictionary<string, string> actual = cache.GetOrganizationBindings();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void AzureReposUserManager_GetRemoteBindings_Users_ReturnsRemoteUsers()
        {
            const string org1 = "org1";
            const string org2 = "org2";
            var remote1 = new Uri("https://dev.azure.com/org/_git/repo1");
            var remote2 = new Uri("https://dev.azure.com/org/_git/repo2");

            var expected = new Dictionary<Uri, string>
            {
                [remote1] = "user1",
                [remote2] = "user2",
            };

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            store.PersistedStore[GetRemoteUserKey(remote1)] = "user1";
            store.PersistedStore[GetRemoteUserKey(remote2)] = "user2";
            store.PersistedStore[GetOrgUserKey(org1)] = "user3";
            store.PersistedStore[GetOrgUserKey(org2)] = "user4";

            IDictionary<Uri, string> actual = cache.GetRemoteBindings();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void AzureReposUserManager_GetOrganizationBindings_Users_ReturnsOrgUsers()
        {
            const string org1 = "org1";
            const string org2 = "org2";
            var remote1 = new Uri("https://dev.azure.com/org/_git/repo1");
            var remote2 = new Uri("https://dev.azure.com/org/_git/repo2");

            var expected = new Dictionary<string, string>
            {
                [org1] = "user3",
                [org2] = "user4",
            };

            var trace = new NullTrace();
            var store = new InMemoryIniStore(StringComparer.OrdinalIgnoreCase);
            var cache = new AzureReposUserManager(trace, store);

            store.PersistedStore[GetRemoteUserKey(remote1)] = "user1";
            store.PersistedStore[GetRemoteUserKey(remote2)] = "user2";
            store.PersistedStore[GetOrgUserKey(org1)] = "user3";
            store.PersistedStore[GetOrgUserKey(org2)] = "user4";

            IDictionary<string, string> actual = cache.GetOrganizationBindings();

            Assert.Equal(expected, actual);
        }

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
