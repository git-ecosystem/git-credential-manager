using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using GitCredentialManager.Authentication.Entra;
using Microsoft.Identity.Client.Extensions.Msal;

namespace GitCredentialManager.Diagnostics
{
    public class EntraAuthenticationDiagnostic : Diagnostic
    {
        public EntraAuthenticationDiagnostic(ICommandContext context)
            : base("Microsoft Entra authentication", context)
        { }

        protected override async Task<bool> RunInternalAsync(StringBuilder log, IList<string> additionalFiles)
        {
            var failures = new List<Exception>();

            await RunCacheDiagnosticAsync("Shared Microsoft developer tools", true, log, failures);
            log.AppendLine();
            await RunCacheDiagnosticAsync("Git Credential Manager", false, log, failures);

            if (failures.Count == 1)
            {
                throw failures[0];
            }

            if (failures.Count > 1)
            {
                throw new AggregateException("Multiple MSAL token cache diagnostics failed.", failures);
            }

            return true;
        }

        private async Task RunCacheDiagnosticAsync(
            string cacheName,
            bool useSharedCache,
            StringBuilder log,
            ICollection<Exception> failures)
        {
            log.AppendLine($"{cacheName} cache");

            var entraAuth = new EntraAuthentication(CommandContext, new PublicClientConfig
            {
                UseSharedCache = useSharedCache,
            });

            try
            {
                log.Append("Gathering MSAL token cache data...");
                StorageCreationProperties cacheProps = entraAuth.CreateUserTokenCacheProps(true);
                log.AppendLine(" OK");
                log.AppendLine($"CacheDirectory: {cacheProps.CacheDirectory}");
                log.AppendLine($"CacheFileName: {cacheProps.CacheFileName}");
                log.AppendLine($"CacheFilePath: {cacheProps.CacheFilePath}");

                if (PlatformUtils.IsMacOS())
                {
                    log.AppendLine($"MacKeyChainAccountName: {cacheProps.MacKeyChainAccountName}");
                    log.AppendLine($"MacKeyChainServiceName: {cacheProps.MacKeyChainServiceName}");
                }
                else if (PlatformUtils.IsLinux())
                {
                    log.AppendLine($"KeyringCollection: {cacheProps.KeyringCollection}");
                    log.AppendLine($"KeyringSchemaName: {cacheProps.KeyringSchemaName}");
                    log.AppendLine($"KeyringSecretLabel: {cacheProps.KeyringSecretLabel}");
                    log.AppendLine($"KeyringAttribute1: ({cacheProps.KeyringAttribute1.Key},{cacheProps.KeyringAttribute1.Value})");
                    log.AppendLine($"KeyringAttribute2: ({cacheProps.KeyringAttribute2.Key},{cacheProps.KeyringAttribute2.Value})");
                }

                log.Append("Creating cache helper...");
                var cacheHelper = await MsalCacheHelper.CreateAsync(cacheProps);
                log.AppendLine(" OK");
                log.Append("Verifying MSAL token cache persistence...");
                cacheHelper.VerifyPersistence();
                log.AppendLine(" OK");
            }
            catch (Exception ex)
            {
                log.AppendLine(" Failed");
                failures.Add(new Exception($"{cacheName} cache diagnostic failed.", ex));
            }
        }
    }
}
