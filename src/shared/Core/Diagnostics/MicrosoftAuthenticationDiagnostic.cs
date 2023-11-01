using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using GitCredentialManager.Authentication;
using Microsoft.Identity.Client.Extensions.Msal;

namespace GitCredentialManager.Diagnostics
{
    public class MicrosoftAuthenticationDiagnostic : Diagnostic
    {
        public MicrosoftAuthenticationDiagnostic(ICommandContext context)
            : base("Microsoft authentication (AAD/MSA)", context)
        { }

        protected override async Task<bool> RunInternalAsync(StringBuilder log, IList<string> additionalFiles)
        {
            var msAuth = new MicrosoftAuthentication(CommandContext);
            log.AppendLine(msAuth.CanUseBroker() ? "Broker is enabled." : "Broker is not enabled.");
            log.AppendLine($"Flow type is: {msAuth.GetFlowType()}");

            log.Append("Gathering MSAL token cache data...");
            StorageCreationProperties cacheProps = msAuth.CreateUserTokenCacheProps(true);
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
            try
            {
                log.Append("Verifying MSAL token cache persistence...");
                cacheHelper.VerifyPersistence();
                log.AppendLine(" OK");
            }
            catch (Exception)
            {
                log.AppendLine(" Failed");
                throw;
            }

            return true;
        }
    }
}
