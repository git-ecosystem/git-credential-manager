
using System;

namespace GitCredentialManager.Interop.Windows
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

        public string OAuthRefreshToken { get; set; }

        public DateTimeOffset? PasswordExpiry { get; set; }

        string ICredential.Account => UserName;
    }
}
