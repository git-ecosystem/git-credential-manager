// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using Xunit;
using Microsoft.Git.CredentialManager.Interop.Linux;

namespace Microsoft.Git.CredentialManager.Tests.Interop.Linux
{
    public class LibsecretCollectionTests
    {
        [PlatformFact(Platform.Linux)]
        public void LibsecretCollection_ReadWriteDelete()
        {
            LibsecretCollection collection = LibsecretCollection.Open();

            // Create a key that is guarenteed to be unique
            string key = $"secretkey-{Guid.NewGuid():N}";
            const string userName = "john.doe";
            const string password = "letmein123";
            var credential = new GitCredential(userName, password);

            try
            {
                // Write
                collection.AddOrUpdate(key, credential);

                // Read
                ICredential outCredential = collection.Get(key);

                Assert.NotNull(outCredential);
                Assert.Equal(credential.UserName, outCredential.UserName);
                Assert.Equal(credential.Password, outCredential.Password);
            }
            finally
            {
                // Ensure we clean up after ourselves even in case of 'get' failures
                collection.Remove(key);
            }
        }

        [PlatformFact(Platform.Linux)]
        public void LibsecretCollection_Get_KeyNotFound_ReturnsNull()
        {
            LibsecretCollection collection = LibsecretCollection.Open();

            // Unique key; guaranteed not to exist!
            string key = Guid.NewGuid().ToString("N");

            ICredential credential = collection.Get(key);
            Assert.Null(credential);
        }

        [PlatformFact(Platform.Linux)]
        public void LibsecretCollection_Remove_KeyNotFound_ReturnsFalse()
        {
            LibsecretCollection collection = LibsecretCollection.Open();

            // Unique key; guaranteed not to exist!
            string key = Guid.NewGuid().ToString("N");

            bool result = collection.Remove(key);
            Assert.False(result);
        }
    }
}
