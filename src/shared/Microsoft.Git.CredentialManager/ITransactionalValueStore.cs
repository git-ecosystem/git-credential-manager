// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
namespace Microsoft.Git.CredentialManager
{
    /// <summary>
    /// Represents a simple key/value store that can accumulate changes before 'committing' them to a persistent
    /// storage location, or be reloaded from that storage at will.
    /// </summary>
    /// <typeparam name="TKey">Type of the key.</typeparam>
    /// <typeparam name="TValue">Type of the value.</typeparam>
    public interface ITransactionalValueStore<in TKey, TValue>
    {
        /// <summary>
        /// Reload the store from persisted storage. Any uncommitted changes will be lost.
        /// </summary>
        void Reload();

        /// <summary>
        /// Commit changes to persisted storage. Any changes made outside of the application will be overwritten.
        /// </summary>
        void Commit();

        /// <summary>
        /// Try and get a value with the specified key from the store.
        /// </summary>
        /// <param name="key">Value key.</param>
        /// <param name="value">Value.</param>
        /// <returns>True if a value for the given key was found, false otherwise.</returns>
        bool TryGetValue(TKey key, out TValue value);

        /// <summary>
        /// Add or update a value for the specified key in the store.
        /// </summary>
        /// <param name="key">Value key.</param>
        /// <param name="value">New value.</param>
        void SetValue(TKey key, TValue value);

        /// <summary>
        /// Remove the value with the specified key from the store.
        /// </summary>
        /// <param name="key"></param>
        void Remove(TKey key);
    }
}
