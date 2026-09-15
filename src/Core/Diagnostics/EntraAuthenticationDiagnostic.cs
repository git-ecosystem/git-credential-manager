using System;
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

        protected override async Task RunInternalAsync(IDiagnosticReporter reporter)
        {
            await RunDiagnosticAsync(reporter, useSharedCache: false);
            await RunDiagnosticAsync(reporter, useSharedCache: true);
        }

        private async Task RunDiagnosticAsync(IDiagnosticReporter reporter, bool useSharedCache)
        {
            var entraAuth = new EntraAuthentication(Context, new PublicClientConfig
            {
                UseSharedCache = useSharedCache,
            });

            reporter.ReportProgress(useSharedCache
                ? "Checking shared Microsoft developer tool MSAL token cache"
                : "Checking Git Credential Manager MSAL token cache");

            StorageCreationProperties cacheProps = entraAuth.CreateUserTokenCacheProps(true);
            reporter.ReportInfo($"CacheDirectory: {cacheProps.CacheDirectory}");
            reporter.ReportInfo($"CacheFileName: {cacheProps.CacheFileName}");
            reporter.ReportInfo($"CacheFilePath: {cacheProps.CacheFilePath}");

            if (PlatformUtils.IsMacOS())
            {
                reporter.ReportInfo($"MacKeyChainAccountName: {cacheProps.MacKeyChainAccountName}");
                reporter.ReportInfo($"MacKeyChainServiceName: {cacheProps.MacKeyChainServiceName}");
            }
            else if (PlatformUtils.IsLinux())
            {
                reporter.ReportInfo($"KeyringCollection: {cacheProps.KeyringCollection}");
                reporter.ReportInfo($"KeyringSchemaName: {cacheProps.KeyringSchemaName}");
                reporter.ReportInfo($"KeyringSecretLabel: {cacheProps.KeyringSecretLabel}");
                reporter.ReportInfo($"KeyringAttribute1: ({cacheProps.KeyringAttribute1.Key},{cacheProps.KeyringAttribute1.Value})");
                reporter.ReportInfo($"KeyringAttribute2: ({cacheProps.KeyringAttribute2.Key},{cacheProps.KeyringAttribute2.Value})");
            }

            reporter.ReportProgress("Creating cache helper");
            var cacheHelper = await MsalCacheHelper.CreateAsync(cacheProps);
            try
            {
                reporter.ReportProgress("Verifying MSAL token cache persistence");
                cacheHelper.VerifyPersistence();
            }
            catch (Exception ex)
            {
                reporter.ReportWarning($"Failed cache persistence test: {ex.Message}");
            }
        }
    }
}
