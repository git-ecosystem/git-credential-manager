using System;
using Xunit;
using Microsoft.Git.CredentialManager.SecureStorage;

namespace Microsoft.Git.CredentialManager.Tests.SecureStorage
{
    public class MacOSKeychainTests
    {
        [PlatformFact(Platform.MacOS)]
        public void MacOSKeychain_ReadWriteDelete()
        {
            MacOSKeychain keychain = MacOSKeychain.OpenDefault();

            // Create a key that is guarenteed to be unique
            string key = $"secretkey-{Guid.NewGuid():N}";
            const string userName = "john.doe";
            const string password = "letmein123";
            var credential = new GitCredential(userName, password);

            try
            {
                // Write
                keychain.AddOrUpdate(key, credential);

                // Read
                ICredential outCredential = keychain.Get(key);

                Assert.NotNull(outCredential);
                Assert.Equal(credential.UserName, outCredential.UserName);
                Assert.Equal(credential.Password, outCredential.Password);
            }
            finally
            {
                // Ensure we clean up after ourselves even in case of 'get' failures
                keychain.Remove(key);
            }
        }

        [PlatformFact(Platform.MacOS)]
        public void MacOSKeychain_Get_KeyNotFound_ReturnsNull()
        {
            MacOSKeychain keychain = MacOSKeychain.OpenDefault();

            // Unique key; guaranteed not to exist!
            string key = Guid.NewGuid().ToString("N");

            ICredential credential = keychain.Get(key);
            Assert.Null(credential);
        }

        [PlatformFact(Platform.MacOS)]
        public void MacOSKeychain_Remove_KeyNotFound_ReturnsFalse()
        {
            MacOSKeychain keychain = MacOSKeychain.OpenDefault();

            // Unique key; guaranteed not to exist!
            string key = Guid.NewGuid().ToString("N");

            bool result = keychain.Remove(key);
            Assert.False(result);
        }
    }
}
