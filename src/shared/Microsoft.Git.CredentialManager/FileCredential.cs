namespace GitCredentialManager
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
