using System;
using System.Diagnostics;

namespace GitCredentialManager.Interop.Linux
{
    public record SecretServiceCredential : ICredential
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

        public string OAuthRefreshToken { get; init; }

        public DateTimeOffset? PasswordExpiry { get; init; }
    }
}
