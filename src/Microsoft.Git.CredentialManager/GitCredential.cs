using System;
using System.Globalization;
using System.Text;

namespace Microsoft.Git.CredentialManager
{
    public class GitCredential
    {
        public GitCredential(string userName, string password)
        {
            UserName = userName;
            Password = password;
        }

        public string UserName { get; }

        public string Password { get; }

        /// <summary>
        /// Returns the base-64 encoded, {username}:{password} formatted string of this `<see cref="GitCredential"/>`.
        /// </summary>
        public string ToBase64String()
        {
            string basicAuthValue = string.Format(CultureInfo.InvariantCulture, "{0}:{1}", UserName, Password);
            byte[] authBytes = Encoding.UTF8.GetBytes(basicAuthValue);
            return Convert.ToBase64String(authBytes);
        }
    }
}
