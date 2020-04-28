// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Git.CredentialManager
{
    /// <summary>
    /// Represents a simple credential; user name and password pair.
    /// </summary>
    public interface ICredential
    {
        /// <summary>
        /// User name.
        /// </summary>
        string UserName { get; }

        /// <summary>
        /// Password.
        /// </summary>
        string Password { get; }
    }

    /// <summary>
    /// Represents a credential (username/password pair) that Git can use to authenticate to a remote repository.
    /// </summary>
    public class GitCredential : ICredential
    {
        public GitCredential(string userName, string password)
        {
            UserName = userName;
            Password = password;
        }

        public string UserName { get; }

        public string Password { get; }
    }
}
