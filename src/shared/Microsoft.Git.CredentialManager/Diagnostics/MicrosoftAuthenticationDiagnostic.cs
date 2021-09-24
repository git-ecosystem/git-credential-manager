using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager.Authentication;
using Microsoft.Identity.Client.Extensions.Msal;

namespace Microsoft.Git.CredentialManager.Diagnostics
{
    public class MicrosoftAuthenticationDiagnostic : Diagnostic
    {
        private readonly ICommandContext _context;

        public MicrosoftAuthenticationDiagnostic(ICommandContext context)
            : base("Microsoft authentication (AAD/MSA)")
        {
            EnsureArgument.NotNull(context, nameof(context));

            _context = context;
        }

        protected override async Task<bool> RunInternalAsync(StringBuilder log, IList<string> additionalFiles)
        {
            if (MicrosoftAuthentication.CanUseBroker(_context))
            {
                log.Append("Checking broker initialization state...");
                if (MicrosoftAuthentication.IsBrokerInitialized)
                {
                    log.AppendLine(" Initialized");
                }
                else
                {
                    log.AppendLine("  Not initialized");
                    log.Append("Initializing broker...");
                    MicrosoftAuthentication.InitializeBroker();
                    log.AppendLine("OK");
                }
            }
            else
            {
                log.AppendLine("Broker not supported.");
            }

            var msAuth = new MicrosoftAuthentication(_context);
            log.AppendLine($"Flow type is: {msAuth.GetFlowType()}");

            log.Append("Gathering MSAL token cache data...");
            StorageCreationProperties cacheProps = msAuth.CreateTokenCacheProps(true);
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
