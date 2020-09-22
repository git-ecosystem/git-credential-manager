// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Git.CredentialManager.Interop.Windows
{
    public class WindowsCredential : ICredential
    {
        public WindowsCredential(string service, string userName, string password, string targetName)
        {
            Service = service;
            UserName = userName;
            Password = password;
            TargetName = targetName;
        }

        public string Service { get; }

        public string UserName { get; }

        public string Password { get; }

        public string TargetName { get; }

        string ICredential.Account => UserName;
    }
}
