// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager
{
    /// <summary>
    /// Represents a particular Git hosting service and provides for the creation of credentials to access the remote.
    /// </summary>
    public interface IHostProvider : IDisposable
    {
        /// <summary>
        /// Unique identifier of the hosting provider.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Name of the hosting provider.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Supported authority identifiers.
        /// </summary>
        IEnumerable<string> SupportedAuthorityIds { get; }

        /// <summary>
        /// Determine if the <see cref="InputArguments"/> are recognized by this particular Git hosting provider.
        /// </summary>
        /// <param name="input">Input arguments of a Git credential query.</param>
        /// <returns>True if the provider supports the Git credential request, false otherwise.</returns>
        bool IsSupported(InputArguments input);

        /// <summary>
        /// Get a credential for accessing the remote Git repository on this hosting service.
        /// </summary>
        /// <param name="input">Input arguments of a Git credential query.</param>
        /// <returns>A credential Git can use to authenticate to the remote repository.</returns>
        Task<ICredential> GetCredentialAsync(InputArguments input);

        /// <summary>
        /// Store a credential for accessing the remote Git repository on this hosting service.
        /// </summary>
        /// <param name="input">Input arguments of a Git credential query.</param>
        Task StoreCredentialAsync(InputArguments input);

        /// <summary>
        /// Erase a stored credential for accessing the remote Git repository on this hosting service.
        /// </summary>
        /// <param name="input">Input arguments of a Git credential query.</param>
        Task EraseCredentialAsync(InputArguments input);
    }

    /// <summary>
    /// Represents a Git hosting provider where credentials can be stored and recalled in/from the Operating System's
    /// secure credential store.
    /// </summary>
    public abstract class HostProvider : IHostProvider
    {
        protected HostProvider(ICommandContext context)
        {
            Context = context;
        }

        /// <summary>
        /// The current command execution context.
        /// </summary>
        protected ICommandContext Context { get; }

        public abstract string Id { get; }

        public abstract string Name { get; }

        public virtual IEnumerable<string> SupportedAuthorityIds => Enumerable.Empty<string>();

        public abstract bool IsSupported(InputArguments input);

        /// <summary>
        /// Return a key that uniquely represents the given Git credential query arguments.
        /// </summary>
        /// <remarks>
        /// This key forms part of the identifier used to retrieve and store credentials from the OS secure
        /// credential storage system. It is important the returned value is stable over time to avoid any
        /// potential re-authentication requests.
        /// </remarks>
        /// <param name="input">Input arguments of a Git credential query.</param>
        /// <returns>Stable credential key.</returns>
        public abstract string GetCredentialKey(InputArguments input);

        /// <summary>
        /// Create a new credential used for accessing the remote Git repository on this hosting service.
        /// </summary>
        /// <param name="input">Input arguments of a Git credential query.</param>
        /// <returns>A credential Git can use to authenticate to the remote repository.</returns>
        public abstract Task<ICredential> GenerateCredentialAsync(InputArguments input);

        public virtual async Task<ICredential> GetCredentialAsync(InputArguments input)
        {
            // Try and locate an existing PAT in the OS credential store
            string credentialKey = GetCredentialKey(input);
            Context.Trace.WriteLine($"Looking for existing credential in store with key '{credentialKey}'...");
            ICredential credential = Context.CredentialStore.Get(credentialKey);

            if (credential == null)
            {
                Context.Trace.WriteLine("No existing credential found.");

                // No existing credential was found, create a new one
                Context.Trace.WriteLine("Creating new credential...");
                credential = await GenerateCredentialAsync(input);
                Context.Trace.WriteLine("Credential created.");
            }
            else
            {
                Context.Trace.WriteLine("Existing credential found.");
            }

            return credential;
        }

        public virtual Task StoreCredentialAsync(InputArguments input)
        {
            // Create the credential based on Git's input
            string userName = input.UserName;
            string password = input.Password;

            // WIA-authentication is signaled to Git as an empty username/password pair
            // and we will get called to 'store' these WIA credentials.
            // We avoid storing empty credentials.
            if (string.IsNullOrWhiteSpace(userName) && string.IsNullOrWhiteSpace(password))
            {
                Context.Trace.WriteLine("Not storing empty credential.");
            }
            else
            {
                var credential = new GitCredential(userName, password);

                // Add or update the credential in the store.
                string credentialKey = GetCredentialKey(input);
                Context.Trace.WriteLine($"Storing credential with key '{credentialKey}'...");
                Context.CredentialStore.AddOrUpdate(credentialKey, credential);
                Context.Trace.WriteLine("Credential was successfully stored.");
            }

            return Task.CompletedTask;
        }

        public virtual Task EraseCredentialAsync(InputArguments input)
        {
            // Try to locate an existing credential with the computed key
            string credentialKey = GetCredentialKey(input);
            Context.Trace.WriteLine($"Looking for existing credential in store with key '{credentialKey}'...");
            ICredential credential = Context.CredentialStore.Get(credentialKey);
            if (credential == null)
            {
                Context.Trace.WriteLine("No stored credential was found.");
                return Task.CompletedTask;
            }
            else
            {
                Context.Trace.WriteLine("Existing credential found.");
            }

            // If we've been given a specific username and/or password we should only proceed
            // to erase the stored credential if they match exactly
            if (!string.IsNullOrWhiteSpace(input.UserName) && !StringComparer.Ordinal.Equals(input.UserName, credential.UserName))
            {
                Context.Trace.WriteLine("Stored username does not match specified username - not erasing credential.");
                Context.Trace.WriteLine($"\tInput  username={input.UserName}");
                Context.Trace.WriteLine($"\tStored username={credential.UserName}");
                return Task.CompletedTask;
            }

            if (!string.IsNullOrWhiteSpace(input.Password) && !StringComparer.Ordinal.Equals(input.Password, credential.Password))
            {
                Context.Trace.WriteLine("Stored password does not match specified password - not erasing credential.");
                Context.Trace.WriteLineSecrets("\tInput  password={0}", new object[] {input.Password});
                Context.Trace.WriteLineSecrets("\tStored password={0}", new object[] {credential.Password});
                return Task.CompletedTask;
            }

            Context.Trace.WriteLine("Erasing stored credential...");
            if (Context.CredentialStore.Remove(credentialKey))
            {
                Context.Trace.WriteLine("Credential was successfully erased.");
            }
            else
            {
                Context.Trace.WriteLine("Credential erase failed.");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when the application is being terminated. Clean up and release any resources.
        /// </summary>
        /// <param name="disposing">True if the instance is being disposed, false if being finalized.</param>
        protected virtual void Dispose(bool disposing) { }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~HostProvider()
        {
            Dispose(false);
        }
    }
}
