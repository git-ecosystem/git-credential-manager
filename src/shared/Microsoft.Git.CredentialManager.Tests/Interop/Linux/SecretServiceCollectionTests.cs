// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using Xunit;
using Microsoft.Git.CredentialManager.Interop.Linux;

namespace Microsoft.Git.CredentialManager.Tests.Interop.Linux
{
    public class SecretServiceCollectionTests
    {
        private const string TestNamespace = "git-test";

        [PlatformFact(Platform.Linux, Skip = "Cannot run headless")]
        public void SecretServiceCollection_ReadWriteDelete()
        {
            var collection = new SecretServiceCollection(TestNamespace);

            // Create a service that is guaranteed to be unique
            string service = $"https://example.com/{Guid.NewGuid():N}";
            const string userName = "john.doe";
            const string password = "letmein123";

            try
            {
                // Write
                collection.AddOrUpdate(service, userName, password);

                // Read
                ICredential outCredential = collection.Get(service, userName);

                Assert.NotNull(outCredential);
                Assert.Equal(userName, userName);
                Assert.Equal(password, outCredential.Password);
            }
            finally
            {
                // Ensure we clean up after ourselves even in case of 'get' failures
                collection.Remove(service, userName);
            }
        }

        [PlatformFact(Platform.Linux, Skip = "Cannot run headless")]
        public void SecretServiceCollection_Get_NotFound_ReturnsNull()
        {
            var collection = new SecretServiceCollection(TestNamespace);

            // Unique service; guaranteed not to exist!
            string service = $"https://example.com/{Guid.NewGuid():N}";

            ICredential credential = collection.Get(service, null);
            Assert.Null(credential);
        }

        [PlatformFact(Platform.Linux, Skip = "Cannot run headless")]
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
