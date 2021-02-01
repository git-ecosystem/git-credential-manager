// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager.Tests.Objects
{
    public class InMemoryIniStore : IScopedTransactionalStore
    {
        private readonly object _txLock = new object();

        public IDictionary<string, string> PersistedStore { get; }

        public IDictionary<string, string> MemoryStore { get; }

        public InMemoryIniStore() : this(EqualityComparer<string>.Default) { }

        public InMemoryIniStore(IEqualityComparer<string> comparer)
            : this(new Dictionary<string, string>(comparer)) { }

        public InMemoryIniStore(Dictionary<string, string> persistedStore)
        {
            PersistedStore = persistedStore;
            MemoryStore = new Dictionary<string, string>(persistedStore.Comparer);

            Reload();
        }

        public Task Reload()
        {
            // Copy state from persisted store -> memory store
            lock (_txLock)
            {
                MemoryStore.Clear();
                foreach (var kvp in PersistedStore)
                {
                    MemoryStore.Add(kvp);
                }
            }

            return Task.CompletedTask;
        }

        public Task Commit()
        {
            // Copy state from memory store -> persisted store
            lock (_txLock)
            {
                PersistedStore.Clear();
                foreach (var kvp in MemoryStore)
                {
                    PersistedStore.Add(kvp);
                }
            }

            return Task.CompletedTask;
        }

        public IEnumerable<string> GetSectionScopes(string sectionName)
        {
            var scopes = new HashSet<string>();

            string prefix = $"{sectionName}.";
            var cmp = StringComparison.OrdinalIgnoreCase;
            foreach (string key in MemoryStore.Keys.Where(x => x.StartsWith(prefix, cmp)))
            {
                int sectionIndex = key.IndexOf(".", cmp);
                int propertyIndex = key.LastIndexOf(".", cmp);

                if (sectionIndex > -1 && propertyIndex > -1)
                {
                    string scope = key.Substring(sectionIndex + 1,
                        propertyIndex - sectionIndex - 1);
                    scopes.Add(scope);
                }
            }

            return scopes;
        }

        public bool TryGetValue(string key, out string value)
        {
            return MemoryStore.TryGetValue(key, out value);
        }

        public void SetValue(string key, string value)
        {
            MemoryStore[key] = value;
        }

        public void Remove(string key)
        {
            MemoryStore.Remove(key);
        }
    }
}
