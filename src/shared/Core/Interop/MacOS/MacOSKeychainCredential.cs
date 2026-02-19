using System;
using System.Diagnostics;

namespace GitCredentialManager.Interop.MacOS
{
    [DebuggerDisplay("{DebuggerDisplay}")]
    public class MacOSKeychainCredential : ICredential
    {
        internal MacOSKeychainCredential(string service, string account, string password, string label)
        {
            Service = service;
            Account = account;
            Password = password;
            Label = label;
        }

        public string Service  { get; }

        public string Account { get; }

        public string Label { get; }

        public string Password { get; }

        public DateTimeOffset? PasswordExpiry => null;

        public string OAuthRefreshToken => null;

        private string DebuggerDisplay => $"{Label} [Service: {Service}, Account: {Account}]";
    }
}
