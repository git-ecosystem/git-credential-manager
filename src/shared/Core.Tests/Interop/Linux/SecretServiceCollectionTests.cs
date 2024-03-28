using System;
using Xunit;
using GitCredentialManager.Interop.Linux;

namespace GitCredentialManager.Tests.Interop.Linux
{
    public class SecretServiceCollectionTests
    {
        private const string TestNamespace = "git-test";

        [PlatformFact(Platforms.Linux, Skip = "Cannot run headless")]
        public void SecretServiceCollection_ReadWriteDelete()
        {
            var collection = new SecretServiceCollection(TestNamespace);

            // Create a service that is guaranteed to be unique
            string service = $"https://example.com/{Guid.NewGuid():N}";
            const string userName = "john.doe";
            const string password = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]
            const string testRefreshToken = "xyzzy";
            DateTimeOffset testExpiry = DateTimeOffset.FromUnixTimeSeconds(1919539847);

            try
            {
                // Write
                collection.AddOrUpdate(service, new GitCredential(userName, password) { PasswordExpiry = testExpiry, OAuthRefreshToken = testRefreshToken});

                // Read
                ICredential outCredential = collection.Get(service, userName);

                Assert.NotNull(outCredential);
                Assert.Equal(userName, userName);
                Assert.Equal(password, outCredential.Password);
                Assert.Equal(testRefreshToken, outCredential.OAuthRefreshToken);
                Assert.Equal(testExpiry, outCredential.PasswordExpiry);
            }
            finally
            {
                // Ensure we clean up after ourselves even in case of 'get' failures
                collection.Remove(service, userName);
            }
        }

        [PlatformFact(Platforms.Linux, Skip = "Cannot run headless")]
        public void SecretServiceCollection_Get_NotFound_ReturnsNull()
        {
            var collection = new SecretServiceCollection(TestNamespace);

            // Unique service; guaranteed not to exist!
            string service = $"https://example.com/{Guid.NewGuid():N}";

            ICredential credential = collection.Get(service, null);
            Assert.Null(credential);
        }

        [PlatformFact(Platforms.Linux, Skip = "Cannot run headless")]
        public void SecretServiceCollection_Remove_NotFound_ReturnsFalse()
        {
            var collection = new SecretServiceCollection(TestNamespace);

            // Unique service; guaranteed not to exist!
            string service = $"https://example.com/{Guid.NewGuid():N}";

            bool result = collection.Remove(service, account: null);
            Assert.False(result);
        }
    }
}
