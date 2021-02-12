// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;

namespace Microsoft.Git.CredentialManager
{
    public class CredentialCacheStore : ICredentialStore
    {
        readonly IGit _git;
        readonly IHelperProcess _helper;

        public CredentialCacheStore(IGit git, IHelperProcess helper)
        {
            _git = git;
            _helper = helper;
        }

        #region ICredentialStore

        public ICredential Get(string service, string account)
        {
            throw new NotImplementedException();
        }

        public void AddOrUpdate(string service, string account, string secret)
        {
            throw new NotImplementedException();
        }

        public bool Remove(string service, string account)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
