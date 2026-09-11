using System.Collections.Generic;

namespace GitCredentialManager
{
    /// <summary>
    /// Represents a secure storage location for <see cref="ICredential"/>s.
    /// </summary>
    public interface ICredentialStore
    {
        /// <summary>
        /// Get the name of the credential store.
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Gets the maximum width of a secret that can be stored by this CredentialStore. When set allows credentials
        /// to be stored in chunks of at most the size configured.
        /// 
        /// A value of LTE 0 (default of 0) indicates there is no limits set by the CredentialStore
        /// </summary>
        /// <remarks>
        /// The size represented here is the number of characters (<see langword="char"/>) of the string.
        /// </remarks>
        int MaxCredentialSize => 0;
        
        /// <summary>
        /// Get all accounts from the store for the given service.
        /// </summary>
        /// <param name="service">Name of the service to match against. Use null to match all values.</param>
        /// <returns>All accounts that match the query.</returns>
        IList<string> GetAccounts(string service);

        /// <summary>
        /// Get the first credential from the store that matches the given query.
        /// </summary>
        /// <param name="service">Name of the service to match against. Use null to match all values.</param>
        /// <param name="account">Account name to match against. Use null to match all values.</param>
        /// <returns>First matching credential or null if none are found.</returns>
        ICredential Get(string service, string account);

        /// <summary>
        /// Add or update credential in the store with the specified key.
        /// </summary>
        /// <param name="service">Name of the service this credential is for. Use null to match all values.</param>
        /// <param name="account">Account associated with this credential. Use null to match all values.</param>
        /// <param name="secret">Secret value to store.</param>
        void AddOrUpdate(string service, string account, string secret);

        /// <summary>
        /// Delete credential from the store that matches the given query.
        /// </summary>
        /// <param name="service">Name of the service to match against. Use null to match all values.</param>
        /// <param name="account">Account name to match against. Use null to match all values.</param>
        /// <returns>True if the credential was deleted, false otherwise.</returns>
        bool Remove(string service, string account);
    }
}
