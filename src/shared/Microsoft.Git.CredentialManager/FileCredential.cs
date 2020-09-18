// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
namespace Microsoft.Git.CredentialManager
{
    public class FileCredential : ICredential
    {
        public FileCredential(string fullPath, string service, string account, string password)
        {
            FullPath = fullPath;
            Service = service;
            Account = account;
            Password = password;
        }

        public string FullPath { get; }

        public string Service { get; }

        public string Account { get; }

        public string Password { get; }
    }
}
