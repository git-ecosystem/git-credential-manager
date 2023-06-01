using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GitCredentialManager.Interop.Linux;
using GitCredentialManager.Interop.MacOS;
using GitCredentialManager.Interop.Posix;
using GitCredentialManager.Interop.Windows;
using StoreNames = GitCredentialManager.Constants.CredentialStoreNames;

namespace GitCredentialManager
{
    public class CredentialStore : ICredentialStore
    {
        private readonly ICommandContext _context;

        private ICredentialStore _backingStore;

        public CredentialStore(ICommandContext context)
        {
            EnsureArgument.NotNull(context, nameof(context));

            _context = context;
        }

        #region ICredentialStore

        public IList<string> GetAccounts(string service)
        {
            EnsureBackingStore();
            return _backingStore.GetAccounts(service);
        }

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

            string ns = _context.Settings.CredentialNamespace;
            string credStoreName = _context.Settings.CredentialBackingStore?.ToLowerInvariant()
                                ?? GetDefaultStore();

            switch (credStoreName)
            {
                case StoreNames.WindowsCredentialManager:
                    ValidateWindowsCredentialManager();
                    _backingStore = new WindowsCredentialManager(ns);
                    break;

                case StoreNames.Dpapi:
                    ValidateDpapi(out string dpapiStoreRoot);
                    _backingStore = new DpapiCredentialStore(_context.FileSystem, dpapiStoreRoot, ns);
                    break;

                case StoreNames.MacOSKeychain:
                    ValidateMacOSKeychain();
                    _backingStore = new MacOSKeychain(ns);
                    break;

                case StoreNames.SecretService:
                    ValidateSecretService();
                    _backingStore = new SecretServiceCollection(ns);
                    break;

                case StoreNames.Gpg:
                    ValidateGpgPass(out string gpgStoreRoot, out string gpgExec);
                    IGpg gpg = new Gpg(gpgExec, _context.SessionManager, _context.ProcessManager, _context.Trace2);
                    _backingStore = new GpgPassCredentialStore(_context.FileSystem, gpg, gpgStoreRoot, ns);
                    break;

                case StoreNames.Cache:
                    ValidateCredentialCache(out string options);
                    _backingStore = new CredentialCacheStore(_context.Git, options);
                    break;

                case StoreNames.Plaintext:
                    ValidatePlaintext(out string plainStoreRoot);
                    _backingStore = new PlaintextCredentialStore(_context.FileSystem, plainStoreRoot, ns);
                    break;

                default:
                    var sb = new StringBuilder();
                    sb.AppendLine(string.IsNullOrWhiteSpace(credStoreName)
                        ? "No credential store has been selected."
                        : $"Unknown credential store '{credStoreName}'.");
                    _context.Trace2.WriteError(sb.ToString());
                    sb.AppendFormat(
                        "{3}Set the {0} environment variable or the {1}.{2} Git configuration setting to one of the following options:{3}{3}",
                        Constants.EnvironmentVariables.GcmCredentialStore,
                        Constants.GitConfiguration.Credential.SectionName,
                        Constants.GitConfiguration.Credential.CredentialStore,
                        Environment.NewLine);
                    AppendAvailableStoreList(sb);
                    sb.AppendLine();
                    sb.AppendLine($"See {Constants.HelpUrls.GcmCredentialStores} for more information.");
                    throw new Exception(sb.ToString());
            }
        }

        private static string GetDefaultStore()
        {
            if (PlatformUtils.IsWindows())
                return StoreNames.WindowsCredentialManager;

            if (PlatformUtils.IsMacOS())
                return StoreNames.MacOSKeychain;

            // Other platforms have no default store
            return null;
        }

        private static void AppendAvailableStoreList(StringBuilder sb)
        {
            if (PlatformUtils.IsWindows())
            {
                sb.AppendFormat("  {1,-13} : Windows Credential Manager (not available over network/SSH sessions){0}",
                    Environment.NewLine, StoreNames.WindowsCredentialManager);

                sb.AppendFormat("  {1,-13} : DPAPI protected files{0}",
                    Environment.NewLine, StoreNames.Dpapi);
            }

            if (PlatformUtils.IsMacOS())
            {
                sb.AppendFormat("  {1,-13} : macOS Keychain{0}",
                    Environment.NewLine, StoreNames.MacOSKeychain);
            }

            if (PlatformUtils.IsLinux())
            {
                sb.AppendFormat("  {1,-13} : freedesktop.org Secret Service (requires graphical interface){0}",
                    Environment.NewLine, StoreNames.SecretService);
            }

            if (PlatformUtils.IsPosix())
            {
                sb.AppendFormat("  {1,-13} : GNU `pass` compatible credential storage (requires GPG and `pass`){0}",
                    Environment.NewLine, StoreNames.Gpg);
            }

            if (!PlatformUtils.IsWindows())
            {
                sb.AppendFormat("  {1,-13} : Git's in-memory credential cache{0}",
                    Environment.NewLine, StoreNames.Cache);
            }

            sb.AppendFormat("  {1,-13} : store credentials in plain-text files (UNSECURE){0}",
                Environment.NewLine, StoreNames.Plaintext);
        }

        private void ValidateWindowsCredentialManager()
        {
            if (!PlatformUtils.IsWindows())
            {
                var message = $"Can only use the '{StoreNames.WindowsCredentialManager}' credential store on Windows.";
                _context.Trace2.WriteError(message);
                throw new Exception(message + Environment.NewLine +
                            $"See {Constants.HelpUrls.GcmCredentialStores} for more information."
                );
            }

            if (!WindowsCredentialManager.CanPersist())
            {
                var message = $"Unable to persist credentials with the '{StoreNames.WindowsCredentialManager}' credential store.";
                _context.Trace2.WriteError(message);
                throw new Exception(message + Environment.NewLine +
                    $"See {Constants.HelpUrls.GcmCredentialStores} for more information."
                );
            }
        }

        private void ValidateDpapi(out string storeRoot)
        {
            if (!PlatformUtils.IsWindows())
            {
                var message = $"Can only use the '{StoreNames.Dpapi}' credential store on Windows.";
                _context.Trace2.WriteError(message);
                throw new Exception(message  + Environment.NewLine +
                    $"See {Constants.HelpUrls.GcmCredentialStores} for more information."
                );
            }

            // Check for a redirected credential store location
            if (!_context.Settings.TryGetSetting(
                Constants.EnvironmentVariables.GcmDpapiStorePath,
                Constants.GitConfiguration.Credential.SectionName,
                Constants.GitConfiguration.Credential.DpapiStorePath,
                out storeRoot))
            {
                // Use default store root at ~/.gcm/dpapi_store
                storeRoot = Path.Combine(_context.FileSystem.UserDataDirectoryPath, "dpapi_store");
            }
        }

        private void ValidateMacOSKeychain()
        {
            if (!PlatformUtils.IsMacOS())
            {
                var message = $"Can only use the '{StoreNames.MacOSKeychain}' credential store on macOS.";
                _context.Trace2.WriteError(message);
                throw new Exception(message  + Environment.NewLine +
                                    $"See {Constants.HelpUrls.GcmCredentialStores} for more information."
                );
            }
        }

        private void ValidateSecretService()
        {
            if (!PlatformUtils.IsLinux())
            {
                var message = $"Can only use the '{StoreNames.SecretService}' credential store on Linux.";
                _context.Trace2.WriteError(message);
                throw new Exception(message + Environment.NewLine +
                                    $"See {Constants.HelpUrls.GcmCredentialStores} for more information."
                );
            }

            if (!_context.SessionManager.IsDesktopSession)
            {
                var message = $"Cannot use the '{StoreNames.SecretService}' credential backing store without a graphical interface present.";
                _context.Trace2.WriteError(message);
                throw new Exception(message + Environment.NewLine +
                                    $"See {Constants.HelpUrls.GcmCredentialStores} for more information."
                );
            }
        }

        private void ValidateGpgPass(out string storeRoot, out string execPath)
        {
            if (!PlatformUtils.IsPosix())
            {
                var message = $"Can only use the '{StoreNames.Gpg}' credential store on POSIX systems.";
                _context.Trace2.WriteError(message);
                throw new Exception(message + Environment.NewLine +
                                    $"See {Constants.HelpUrls.GcmCredentialStores} for more information."
                );
            }

            execPath = GetGpgPath();

            // If we are in a headless environment, and don't have the GPG_TTY or SSH_TTY
            // variables set, then error - we need a TTY device path for pin-entry to work headless.
            if (!_context.SessionManager.IsDesktopSession &&
                !_context.Environment.Variables.ContainsKey("GPG_TTY") &&
                !_context.Environment.Variables.ContainsKey("SSH_TTY"))
            {
                var message = "GPG_TTY is not set; add `export GPG_TTY=$(tty)` to your profile.";
                _context.Trace2.WriteError(message);
                throw new Exception(message + Environment.NewLine +
                                    $"See {Constants.HelpUrls.GcmCredentialStores} for more information."
                );
            }

            // Check for a redirected pass store location
            if (!_context.Settings.TryGetSetting(
                GpgPassCredentialStore.PasswordStoreDirEnvar,
                null, null,
                out storeRoot))
            {
                // Use default store root at ~/.password-store
                storeRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".password-store");
            }

            // Check we have a GPG ID to sign credential files with
            string gpgIdFile = Path.Combine(storeRoot, ".gpg-id");
            if (!_context.FileSystem.FileExists(gpgIdFile))
            {
                var format =
                    "Password store has not been initialized at '{0}'; run `pass init <gpg-id>` to initialize the store.";
                var message = string.Format(format, storeRoot);
                _context.Trace2.WriteError(message);
                throw new Exception(message + Environment.NewLine +
                                    $"See {Constants.HelpUrls.GcmCredentialStores} for more information."
                );
            }
        }

        private void ValidateCredentialCache(out string options)
        {
            if (PlatformUtils.IsWindows())
            {
                var message = $"Can not use the '{StoreNames.Cache}' credential store on Windows due to lack of UNIX socket support in Git for Windows.";
                _context.Trace2.WriteError(message);
                throw new Exception(message + Environment.NewLine +
                                    $"See {Constants.HelpUrls.GcmCredentialStores} for more information."
                );
            }

            // allow for --timeout and other options
            if (!_context.Settings.TryGetSetting(
                Constants.EnvironmentVariables.GcmCredCacheOptions,
                Constants.GitConfiguration.Credential.SectionName,
                Constants.GitConfiguration.Credential.CredCacheOptions,
                out options))
            {
                options = string.Empty;
            }
        }

        private void ValidatePlaintext(out string storeRoot)
        {
            // Check for a redirected credential store location
            if (!_context.Settings.TryGetSetting(
                Constants.EnvironmentVariables.GcmPlaintextStorePath,
                Constants.GitConfiguration.Credential.SectionName,
                Constants.GitConfiguration.Credential.PlaintextStorePath,
                out storeRoot))
            {
                // Use default store root at ~/.gcm/store
                storeRoot = Path.Combine(_context.FileSystem.UserDataDirectoryPath, "store");
            }
        }

        private string GetGpgPath()
        {
            string gpgPath;

            // Use the GCM_GPG_PATH environment variable if set
            if (_context.Environment.Variables.TryGetValue(Constants.EnvironmentVariables.GpgExecutablePath,
                out gpgPath))
            {
                if (_context.FileSystem.FileExists(gpgPath))
                {
                    _context.Trace.WriteLine($"Using Git executable from GCM_GPG_PATH: {gpgPath}");
                    return gpgPath;
                }

                var format = "GPG executable does not exist with path '{0}'";
                var message = string.Format(format, gpgPath);
                throw new Trace2Exception(_context.Trace2, message, format);
            }

            // If no explicit GPG path is specified, mimic the way `pass`
            // determines GPG dependency (use gpg2 if available, otherwise gpg)
            if (_context.Environment.TryLocateExecutable("gpg2", out string gpg2Path))
            {
                _context.Trace.WriteLine($"Using PATH-located GPG (gpg2) executable: {gpg2Path}");
                return gpg2Path;
            }

            gpgPath = _context.Environment.LocateExecutable("gpg");
            _context.Trace.WriteLine($"Using PATH-located GPG (gpg) executable: {gpgPath}");
            return gpgPath;
        }
    }
}
