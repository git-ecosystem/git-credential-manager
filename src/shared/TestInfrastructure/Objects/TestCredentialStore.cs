// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Git.CredentialManager.Tests.Objects
{
    public class TestCredentialStore : ICredentialStore, IDictionary<string, ICredential>
    {
        private readonly IDictionary<string, ICredential> _store;

        public TestCredentialStore()
        {
            _store = new Dictionary<string, ICredential>(StringComparer.Ordinal);
        }

        #region ICredentialStore

        ICredential ICredentialStore.Get(string key)
        {
            if (_store.TryGetValue(key, out var credential))
            {
                return credential;
            }

            return null;
        }

        void ICredentialStore.AddOrUpdate(string key, ICredential credential)
        {
            _store[key] = credential;
        }

        public void Add(string key, ICredential value)
        {
            _store.Add(key, value);
        }

        public bool ContainsKey(string key)
        {
            return _store.ContainsKey(key);
        }

        public bool Remove(string key)
        {
            return _store.Remove(key);
        }

        public bool TryGetValue(string key, out ICredential value)
        {
            return _store.TryGetValue(key, out value);
        }

        public ICredential this[string key]
        {
            get => _store[key];
            set => _store[key] = value;
        }

        public ICollection<string> Keys => _store.Keys;

        public ICollection<ICredential> Values => _store.Values;

        bool ICredentialStore.Remove(string key)
        {
            return _store.Remove(key);
        }

        #endregion

        #region IDictionary<string, ICredential>

        public IEnumerator<KeyValuePair<string, ICredential>> GetEnumerator()
        {
            return _store.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _store).GetEnumerator();
        }

        public void Add(KeyValuePair<string, ICredential> item)
        {
            _store.Add(item);
        }

        public void Clear()
        {
            _store.Clear();
        }

        public bool Contains(KeyValuePair<string, ICredential> item)
        {
            return _store.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, ICredential>[] array, int arrayIndex)
        {
            _store.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, ICredential> item)
        {
            return _store.Remove(item);
        }

        public int Count => _store.Count;

        public bool IsReadOnly => _store.IsReadOnly;

        #endregion
    }
}
