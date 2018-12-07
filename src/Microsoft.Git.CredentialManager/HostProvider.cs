// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager
{
    /// <summary>
    /// Represents a particular Git hosting service and provides for the creation of credentials to access the remote.
    /// </summary>
    public interface IHostProvider
    {
        /// <summary>
        /// Name of the hosting provider.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Determine if the <see cref="InputArguments"/> are recognized by this particular Git hosting provider.
        /// </summary>
        /// <param name="input">Input arguments of a Git credential query.</param>
        /// <returns>True if the provider supports the Git credential request, false otherwise.</returns>
        bool IsSupported(InputArguments input);

        /// <summary>
        /// Return a key that uniquely represents the given Git credential query arguments.
        /// </summary>
        /// <remarks>
        /// This key forms part of the identifier used to retrieve and store credentials from the OS secure
        /// credential storage system. It is important the returned value is stable over time to avoid any
        /// potential re-authentication requests.
        /// </remarks>
        /// <param name="input">Input arguments of a Git credential query.</param>
        /// <returns></returns>
        string GetCredentialKey(InputArguments input);

        /// <summary>
        /// Create a new credential for accessing the remote Git repository on this hosting service.
        /// </summary>
        /// <param name="input">Input arguments of a Git credential query.</param>
        /// <returns>A new credential Git can use to authenticate to the remote repository.</returns>
        Task<GitCredential> CreateCredentialAsync(InputArguments input);

        /// <summary>
        /// Whether or not <see cref="GitCredential"/>s created by this provider should be stored in the
        /// secure credential storage system when the call to <see cref="CreateCredentialAsync"/> is made.
        /// </summary>
        /// <remarks>
        /// Some host providers may wish to or need to store <see cref="GitCredential"/>s created by them
        /// immediately after they are created, rather than at a later time when requested to do so by Git.
        /// One example reason for this is that the provider is unable to create the same credential key
        /// due to missing information in the <see cref="InputArguments"/> on subsequent calls to
        /// <see cref="GetCredentialKey"/>.
        /// <para/>
        /// If this property returns true, Git Credential Manager will store any credential created by this
        /// provider during the `git credential-helper get` call, rather than in a `store` call (which will
        /// now be a no-op).
        /// </remarks>
        bool IsCredentialStoredOnCreation { get; }
    }

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

        public abstract string Name { get; }

        public abstract bool IsSupported(InputArguments input);

        public abstract string GetCredentialKey(InputArguments input);

        public abstract Task<GitCredential> CreateCredentialAsync(InputArguments input);

        public abstract bool IsCredentialStoredOnCreation { get; }
    }
}
