// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.IO;
using System.Text;
using Microsoft.Git.CredentialManager.Interop.Posix;

namespace Microsoft.Git.CredentialManager.Interop.Linux
{
    public class LinuxCredentialStore : ICredentialStore
    {
        private const string SecretServiceStoreOption = "secretservice";
        private const string GpgStoreOption = "gpg";
        private const string PlaintextStoreOption = "plaintext";

        private readonly IFileSystem _fileSystem;
        private readonly ISettings _settings;
        private readonly ISessionManager _sessionManager;
        private readonly IGpg _gpg;
        private readonly IEnvironment _environment;

        private ICredentialStore _backingStore;

        public LinuxCredentialStore(IFileSystem fileSystem, ISettings settings, ISessionManager sessionManager, IGpg gpg, IEnvironment environment)
        {
            EnsureArgument.NotNull(fileSystem, nameof(fileSystem));
            EnsureArgument.NotNull(settings, nameof(settings));
            EnsureArgument.NotNull(sessionManager, nameof(sessionManager));
            EnsureArgument.NotNull(gpg, nameof(gpg));
            EnsureArgument.NotNull(environment, nameof(environment));

            _fileSystem = fileSystem;
            _settings = settings;
            _sessionManager = sessionManager;
            _gpg = gpg;
            _environment = environment;
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
                case SecretServiceStoreOption:
                    ValidateSecretService();
                    _backingStore = new SecretServiceCollection(ns);
                    break;

                case GpgStoreOption:
                    ValidateGpgPass(out string gpgStoreRoot);
                    _backingStore = new GpgPassCredentialStore(_fileSystem, _gpg, gpgStoreRoot, ns);
                    break;

                case PlaintextStoreOption:
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
                    sb.AppendFormat("  {0,-13} : freedesktop.org Secret Service (requires graphical interface){1}", SecretServiceStoreOption, Environment.NewLine);
                    sb.AppendFormat("  {0,-13} : GNU `pass` compatible credential storage (requires GPG and `pass`){1}", GpgStoreOption, Environment.NewLine);
                    sb.AppendFormat("  {0,-13} : store credentials in plain-text files (UNSECURE){1}", PlaintextStoreOption, Environment.NewLine);
                    sb.AppendLine();
                    sb.AppendLine($"See {Constants.HelpUrls.GcmLinuxCredStores} for more information.");
                    throw new Exception(sb.ToString());
            }
        }

        private void ValidateSecretService()
        {
            if (!_sessionManager.IsDesktopSession)
            {
                throw new Exception($"Cannot use the '{SecretServiceStoreOption}' credential backing store without a graphical interface present." +
                                    Environment.NewLine + $"See {Constants.HelpUrls.GcmLinuxCredStores} for more information.");
            }
        }

        private void ValidateGpgPass(out string storeRoot)
        {
            // If we are in a headless environment, and don't have the GPG_TTY or SSH_TTY
            // variables set, then error - we need a TTY device path for pin-entry to work headless.
            if (!_sessionManager.IsDesktopSession &&
                !_environment.Variables.ContainsKey("GPG_TTY") &&
                !_environment.Variables.ContainsKey("SSH_TTY"))
            {
                throw new Exception("GPG_TTY is not set; add `export GPG_TTY=$(tty)` to your profile." +
                                    Environment.NewLine + $"See {Constants.HelpUrls.GcmLinuxCredStores} for more information.");
            }

            // Check for a redirected pass store location
            if (!_settings.TryGetSetting(
                GpgPassCredentialStore.PasswordStoreDirEnvar,
                null, null,
                out storeRoot))
            {
                // Use default store root at ~/.password-store
                storeRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".password-store");
            }

            // Check we have a GPG ID to sign credential files with
            string gpgIdFile = Path.Combine(storeRoot, ".gpg-id");
            if (!_fileSystem.FileExists(gpgIdFile))
            {
                throw new Exception($"Password store has not been initialized at '{storeRoot}'; run `pass init <gpg-id>` to initialize the store." +
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
