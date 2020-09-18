// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.IO;
using System.Text;

namespace Microsoft.Git.CredentialManager.Interop.Linux
{
    public class LinuxCredentialStore : ICredentialStore
    {
        private readonly IFileSystem _fileSystem;
        private readonly ISettings _settings;
        private readonly ISessionManager _sessionManager;

        private ICredentialStore _backingStore;

        public LinuxCredentialStore(IFileSystem fileSystem, ISettings settings, ISessionManager sessionManager)
        {
            EnsureArgument.NotNull(fileSystem, nameof(fileSystem));
            EnsureArgument.NotNull(settings, nameof(settings));
            EnsureArgument.NotNull(sessionManager, nameof(sessionManager));

            _fileSystem = fileSystem;
            _settings = settings;
            _sessionManager = sessionManager;
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
            if (_backingStore != null)
            {
                return;
            }

            string ns = _settings.CredentialNamespace;
            string credStoreName = _settings.CredentialBackingStore?.ToLowerInvariant();

            switch (credStoreName)
            {
                case "secretservice":
                    ValidateSecretService();
                    _backingStore = new SecretServiceCollection(ns);
                    break;

                case "plaintext":
                    ValidatePlaintext(out string plainStoreRoot);
                    _backingStore = new PlaintextCredentialStore(_fileSystem, plainStoreRoot, ns);
                    break;

                default:
                    var sb = new StringBuilder();
                    sb.AppendLine("No credential backing store has been selected.");
                    sb.AppendFormat(
                        "{3}Set the {0} environment variable or the {1}.{2} Git configuration setting to one of the following options:{3}{3}",
                        Constants.EnvironmentVariables.GcmCredentialStore,
                        Constants.GitConfiguration.Credential.SectionName,
                        Constants.GitConfiguration.Credential.CredentialStore,
                        Environment.NewLine);
                    sb.AppendLine("  secretservice : freedesktop.org Secret Service (requires graphical interface)");
                    sb.AppendLine("  plaintext     : store credentials in plain-text files (UNSECURE)");
                    sb.AppendLine();
                    sb.AppendLine($"See {Constants.HelpUrls.GcmLinuxCredStores} for more information.");
                    throw new Exception(sb.ToString());
            }
        }

        private void ValidateSecretService()
        {
            if (!_sessionManager.IsDesktopSession)
            {
                throw new Exception("Cannot use the 'secretservice' credential backing store without a graphical interface present." +
                                    Environment.NewLine + $"See {Constants.HelpUrls.GcmLinuxCredStores} for more information.");
            }
        }

        private void ValidatePlaintext(out string storeRoot)
        {
            // Check for a redirected credential store location
            if (!_settings.TryGetSetting(
                Constants.EnvironmentVariables.GcmPlaintextStorePath,
                Constants.GitConfiguration.Credential.SectionName, Constants.GitConfiguration.Credential.PlaintextStorePath,
                out storeRoot))
            {
                // Use default store root at ~/.gcm/store
                storeRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), Constants.GcmConfigDirectoryName, "store");
            }
        }
    }
}
