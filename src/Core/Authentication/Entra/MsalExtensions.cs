using System;
using Microsoft.Identity.Client;

namespace GitCredentialManager.Authentication.Entra;

internal static class MsalExtensions
{
    extension<T>(BaseAbstractApplicationBuilder<T> builder) where T : BaseAbstractApplicationBuilder<T>
    {
        public T WithTraceLogging(ICommandContext context) =>
            WithTraceLogging(builder, context.Settings.IsMsalTracingEnabled,
                context.Settings.IsSecretTracingEnabled, context.Trace);
        public T WithTraceLogging(bool enable, bool includePii, ITrace trace)
        {
            if (enable)
            {
                return builder.WithLogging((level, message, _) =>
                        trace.WriteLine($"[{level.ToString()}] {message}", memberName: "MSAL"),
                    LogLevel.Verbose,
                    includePii,
                    enableDefaultPlatformLogging: false);
            }

            return (T)builder;
        }
    }

    extension(AcquireTokenSilentParameterBuilder builder)
    {
        public AcquireTokenSilentParameterBuilder WithMsaPassthroughTransfer(bool enable, IAccount account)
        {
            if (enable && Guid.TryParse(account.HomeAccountId?.TenantId, out Guid homeTenantId) &&
                homeTenantId == Constants.MsaHomeTenantId)
            {
                return builder.WithTenantId(Constants.MsaTransferTenantId.ToString("D"));
            }

            return builder;
        }
    }
}
