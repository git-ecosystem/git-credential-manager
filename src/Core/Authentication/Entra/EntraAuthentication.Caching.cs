using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace GitCredentialManager.Authentication.Entra
{
    public partial class EntraAuthentication
    {
        /// <summary>
        /// Register the user token cache for public clients.
        /// </summary>
        private Task RegisterCacheAsync(IPublicClientApplication app) =>
            RegisterCacheAsync(app.UserTokenCache, CreateUserTokenCacheProps);

        /// <summary>
        /// Register the app token cache for confidential clients.
        /// </summary>
        private Task RegisterCacheAsync(IConfidentialClientApplication app) =>
            RegisterCacheAsync(app.AppTokenCache, CreateAppTokenCacheProps);

        private delegate StorageCreationProperties StoragePropertiesBuilder(bool useLinuxFallback);

        private async Task RegisterCacheAsync(ITokenCache cache, StoragePropertiesBuilder propsBuilder)
        {
            Context.Trace.WriteLine("Configuring MSAL token cache...");

            if (!PlatformUtils.IsWindows() && !PlatformUtils.IsPosix())
            {
                string osType = PlatformUtils.GetPlatformInformation(Context.Trace2).OperatingSystemType;
                Context.Trace.WriteLine($"Token cache integration is not supported on {osType}.");
                return;
            }

            // We use the MSAL extension library to provide us consistent cache file access semantics (synchronisation, etc)
            // as other GCM processes, and other Microsoft developer tools such as the Azure PowerShell CLI.
            MsalCacheHelper helper = null;
            try
            {
                StorageCreationProperties storageProps = propsBuilder(useLinuxFallback: false);
                helper = await MsalCacheHelper.CreateAsync(storageProps);

                // Test that cache access is working correctly
                helper.VerifyPersistence();
            }
            catch (MsalCachePersistenceException ex)
            {
                var message = "Cannot persist Entra authentication data securely!";
                Context.Console.WriteWarning("cannot persist Entra authentication token cache securely!");
                Context.Trace.WriteLine(message);
                Context.Trace.WriteException(ex);
                Context.Trace2.WriteError(message);

                if (PlatformUtils.IsMacOS())
                {
                    // On macOS sometimes the Keychain returns the "errSecAuthFailed" error - we don't know why
                    // but it appears to be something to do with not being able to access the keychain.
                    // Locking and unlocking (or restarting) often fixes this.
                    Context.Console.WriteWarning(
                        "there is a problem accessing the login Keychain - either manually lock and unlock the " +
                        "login Keychain, or restart the computer to remedy this");
                }
                else if (PlatformUtils.IsLinux())
                {
                    // On Linux the SecretService/keyring might not be available so we must fall-back to a plaintext file.
                    Context.Console.WriteWarning("using plain-text fallback token cache");
                    Context.Trace.WriteLine("Using fall-back plaintext token cache on Linux.");
                    StorageCreationProperties storageProps = propsBuilder(useLinuxFallback: true);
                    helper = await MsalCacheHelper.CreateAsync(storageProps);
                }
            }

            if (helper is null)
            {
                Context.Console.WriteError("failed to set up token cache!");
                Context.Trace.WriteLine("Failed to integrate with token cache!");
            }
            else
            {
                helper.RegisterCache(cache);
                Context.Trace.WriteLine("Token cache configured.");
            }
        }

        /// <summary>
        /// Create the properties for the user token cache. This is used by public client applications only.
        /// This cache is shared between GCM processes, and also other Microsoft developer tools such as the Azure
        /// PowerShell CLI.
        /// </summary>
        /// <param name="useLinuxFallback"></param>
        /// <returns></returns>
        internal StorageCreationProperties CreateUserTokenCacheProps(bool useLinuxFallback)
        {
            const string cacheFileName = "msal.cache";
            string cacheDirectory;
            if (PlatformUtils.IsWindows())
            {
                // The shared MSAL cache is located at "%LocalAppData%\.IdentityService\msal.cache" on Windows.
                cacheDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    ".IdentityService"
                );
            }
            else
            {
                // The shared MSAL cache metadata is located at "~/.local/.IdentityService/msal.cache" on UNIX.
                cacheDirectory = Path.Combine(Context.FileSystem.UserHomePath, ".local", ".IdentityService");
            }

            // The keychain is used on macOS with the following service & account names
            var builder = new StorageCreationPropertiesBuilder(cacheFileName, cacheDirectory)
                .WithMacKeyChain("Microsoft.Developer.IdentityService", "MSALCache");

            if (useLinuxFallback)
            {
                builder.WithLinuxUnprotectedFile();
            }
            else
            {
                // The SecretService/keyring is used on Linux with the following collection name and attributes
                builder.WithLinuxKeyring(cacheFileName,
                    "default", "MSALCache",
                    new KeyValuePair<string, string>("MsalClientID", "Microsoft.Developer.IdentityService"),
                    new KeyValuePair<string, string>("Microsoft.Developer.IdentityService", "1.0.0.0"));
            }

            return builder.Build();
        }

        /// <summary>
        /// Create the properties for the application token cache. This is used by confidential client applications only
        /// and is not shared between applications other than GCM.
        /// </summary>
        internal StorageCreationProperties CreateAppTokenCacheProps(bool useLinuxFallback)
        {
            const string cacheFileName = "app.cache";

            // The confidential client MSAL cache is located at "%UserProfile%\.gcm\msal\app.cache" on Windows
            // and at "~/.gcm/msal/app.cache" on UNIX.
            string cacheDirectory = Path.Combine(Context.FileSystem.UserDataDirectoryPath, "msal");

            // The keychain is used on macOS with the following service & account names
            var builder = new StorageCreationPropertiesBuilder(cacheFileName, cacheDirectory)
                .WithMacKeyChain("GitCredentialManager.MSAL", "AppCache");

            if (useLinuxFallback)
            {
                builder.WithLinuxUnprotectedFile();
            }
            else
            {
                // The SecretService/keyring is used on Linux with the following collection name and attributes
                builder.WithLinuxKeyring(cacheFileName,
                    "default", "AppCache",
                    new KeyValuePair<string, string>("MsalClientID", "GitCredentialManager.MSAL"),
                    new KeyValuePair<string, string>("GitCredentialManager.MSAL", "1.0.0.0"));
            }

            return builder.Build();
        }
    }
}
