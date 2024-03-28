
namespace GitCredentialManager.Interop.Windows
{
    public record WindowsCredential : ICredential
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

        public string OAuthRefreshToken { get; init; }

        string ICredential.Account => UserName;
    }
}
