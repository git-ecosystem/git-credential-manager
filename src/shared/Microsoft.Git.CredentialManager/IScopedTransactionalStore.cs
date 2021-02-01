// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager
{
    /// <summary>
    /// Represents a simple section-scoped property/value store that can accumulate changes before 'committing'
    /// them to a persistent storage location, or be reloaded from that storage at will.
    /// </summary>
    public interface IScopedTransactionalStore
    {
        /// <summary>
        /// Reload the store from persisted storage. Any uncommitted changes will be lost.
        /// </summary>
        Task Reload();

        /// <summary>
        /// Commit changes to persisted storage. Any changes made outside of the application will be overwritten.
        /// </summary>
        Task Commit();

        /// <summary>
        /// Get all scopes present for the given section name.
        /// </summary>
        /// <param name="sectionName">Name of section.</param>
        /// <returns>All scopes present for the specified section name in the store.</returns>
        IEnumerable<string> GetSectionScopes(string sectionName);

        /// <summary>
        /// Try to get the value of a property for the given key.
        /// </summary>
        /// <param name="key">Property key.</param>
        /// <param name="value">Property value.</param>
        /// <returns>True if a property with the specified key exists, false otherwise.</returns>
        bool TryGetValue(string key, out string value);

        /// <summary>
        /// Set the value of a property for the given key. Existing values are overwritten.
        /// </summary>
        /// <param name="key">Property key.</param>
        /// <param name="value">Property value.</param>
        void SetValue(string key, string value);

        /// <summary>
        /// Delete a property identified by the given key if it exists.
        /// </summary>
        /// <param name="key">Property key.</param>
        void Remove(string key);
    }
}
