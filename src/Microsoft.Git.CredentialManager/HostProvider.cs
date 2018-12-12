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
    }
}
