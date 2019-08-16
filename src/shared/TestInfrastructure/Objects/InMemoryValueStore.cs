// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Collections.Generic;

namespace Microsoft.Git.CredentialManager.Tests.Objects
{
    public class InMemoryValueStore<TKey, TValue> : ITransactionalValueStore<TKey, TValue>
    {
        private readonly object _txLock = new object();

        public IDictionary<TKey, TValue> PersistedStore { get; }

        public IDictionary<TKey, TValue> MemoryStore { get; }

        public InMemoryValueStore() : this(EqualityComparer<TKey>.Default) { }

        public InMemoryValueStore(IEqualityComparer<TKey> comparer)
            : this(new Dictionary<TKey, TValue>(comparer)) { }

        public InMemoryValueStore(Dictionary<TKey, TValue> persistedStore)
        {
            PersistedStore = persistedStore;
            MemoryStore = new Dictionary<TKey, TValue>(persistedStore.Comparer);

            Reload();
        }

        public void Reload()
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
        }

        public void Commit()
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
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return MemoryStore.TryGetValue(key, out value);
        }

        public void SetValue(TKey key, TValue value)
        {
            MemoryStore[key] = value;
        }

        public void Remove(TKey key)
        {
            MemoryStore.Remove(key);
        }
    }
}
