using System;
using System.Diagnostics;

namespace GitCredentialManager.Interop.Linux
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

        public string OAuthRefreshToken { get; set; }

        public DateTimeOffset? PasswordExpiry { get; set; }

        private string DebuggerDisplay => $"[Service: {Service}, Account: {Account}]";
    }
}
