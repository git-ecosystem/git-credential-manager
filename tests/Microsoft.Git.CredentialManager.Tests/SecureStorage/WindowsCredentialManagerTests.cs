using System;
using Xunit;
using Microsoft.Git.CredentialManager.SecureStorage;

namespace Microsoft.Git.CredentialManager.Tests.SecureStorage
{
    public class WindowsCredentialManagerTests
    {
        [PlatformFact(Platform.Windows)]
        public void WindowsCredentialManager_ReadWriteDelete()
        {
            WindowsCredentialManager credManager = WindowsCredentialManager.OpenDefault();

            // Create a key that is guarenteed to be unique
            string key = $"secretkey-{Guid.NewGuid():N}";
            const string userName = "john.doe";
            const string password = "letmein123";
            var credential = new GitCredential(userName, password);

            try
            {
                // Write
                credManager.AddOrUpdate(key, credential);

                // Read
                ICredential outCredential = credManager.Get(key);

                Assert.NotNull(outCredential);
                Assert.Equal(credential.UserName, outCredential.UserName);
                Assert.Equal(credential.Password, outCredential.Password);
            }
            finally
            {
                // Ensure we clean up after ourselves even in case of 'get' failures
                credManager.Remove(key);
            }
        }

        [PlatformFact(Platform.Windows)]
        public void WindowsCredentialManager_Get_KeyNotFound_ReturnsNull()
        {
            WindowsCredentialManager credManager = WindowsCredentialManager.OpenDefault();

            // Unique key; guaranteed not to exist!
            string key = Guid.NewGuid().ToString("N");

            ICredential credential = credManager.Get(key);
            Assert.Null(credential);
        }

        [PlatformFact(Platform.Windows)]
        public void WindowsCredentialManager_Remove_KeyNotFound_ReturnsFalse()
        {
            WindowsCredentialManager credManager = WindowsCredentialManager.OpenDefault();

            // Unique key; guaranteed not to exist!
            string key = Guid.NewGuid().ToString("N");

            bool result = credManager.Remove(key);
            Assert.False(result);
        }
    }
}
