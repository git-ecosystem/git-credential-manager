// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Git.CredentialManager.Interop.Linux
{
    public class LinuxCredentialStore : ICredentialStore
    {
        private readonly ISettings _settings;
        private readonly IGit _git;
        private readonly string _namespace;

        private ICredentialStore _backingStore;

        public LinuxCredentialStore(ISettings settings, IGit git, string @namespace = null)
        {
            EnsureArgument.NotNull(settings, nameof(settings));
            EnsureArgument.NotNull(git, nameof(git));

            _settings = settings;
            _git = git;
            _namespace = @namespace;
        }

        #region ICredentialStore

        public ICredential Get(string service, string account)
        {
            EnsureBackingStore();
            return _backingStore.Get(service, account);
        }

        public void AddOrUpdate(string service, string account, string secret)
        {
            EnsureBackingStore();
            _backingStore.AddOrUpdate(service, account, secret);
        }

        public bool Remove(string service, string account)
        {
            EnsureBackingStore();
            return _backingStore.Remove(service, account);
        }

        #endregion

        private void EnsureBackingStore()
        {
            if (_backingStore is null)
            {
                // TODO: determine the available backing stores based on the current session
                // TODO: prompt for the desired backing store
                // TODO: store the desired backing store to ~/.gitconfig
                _backingStore = SecretServiceCollection.Open(_namespace);
            }
        }
    }
}
