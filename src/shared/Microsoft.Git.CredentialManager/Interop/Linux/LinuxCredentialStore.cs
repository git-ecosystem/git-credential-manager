// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Git.CredentialManager.Interop.Linux
{
    public class LinuxCredentialStore : ICredentialStore
    {
        private readonly ISettings _settings;
        private readonly IGit _git;

        private ICredentialStore _backingStore;

        public LinuxCredentialStore(ISettings settings, IGit git)
        {
            EnsureArgument.NotNull(settings, nameof(settings));
            EnsureArgument.NotNull(git, nameof(git));

            _settings = settings;
            _git = git;
        }

        #region ICredentialStore

        public ICredential Get(string key)
        {
            EnsureBackingStore();
            return _backingStore.Get(key);
        }

        public void AddOrUpdate(string key, ICredential credential)
        {
            EnsureBackingStore();
            _backingStore.AddOrUpdate(key, credential);
        }

        public bool Remove(string key)
        {
            EnsureBackingStore();
            return _backingStore.Remove(key);
        }

        #endregion

        private void EnsureBackingStore()
        {
            if (_backingStore is null)
            {
                // TODO: determine the available backing stores based on the current session
                // TODO: prompt for the desired backing store
                // TODO: store the desired backing store to ~/.gitconfig
                _backingStore = SecretServiceCollection.Open();
            }
        }
    }
}
