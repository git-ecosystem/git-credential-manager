// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Diagnostics;

namespace Microsoft.Git.CredentialManager.Interop.Linux
{
    [DebuggerDisplay("{DebuggerDisplay}")]
    public class SecretServiceCredential : ICredential
    {
        internal SecretServiceCredential(string service, string account, string password)
        {
            Service = service;
            Account = account;
            Password = password;
        }

        public string Service { get; }

        public string Account { get; }

        public string Password { get; }

        private string DebuggerDisplay => $"[Service: {Service}, Account: {Account}]";
    }
}
