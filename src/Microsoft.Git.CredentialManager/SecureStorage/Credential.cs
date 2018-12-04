// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
namespace Microsoft.Git.CredentialManager.SecureStorage
{
    /// <summary>
    /// Represents a simple credential; user name and password pair.
    /// </summary>
    public interface ICredential
    {
        string UserName { get; }
        string Password { get; }
    }

    internal class Credential : ICredential
    {
        public Credential(string userName, string password)
        {
            UserName = userName;
            Password = password;
        }

        public string UserName { get; }
        public string Password { get; }
    }
}
