using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace GitCredentialManager.Diagnostics
{
    public class EnvironmentDiagnostic : Diagnostic
    {
        public EnvironmentDiagnostic(ICommandContext commandContext)
            : base("Environment", commandContext)
        { }

        protected override Task<bool> RunInternalAsync(StringBuilder log, IList<string> additionalFiles)
        {
            PlatformInformation platformInfo = PlatformUtils.GetPlatformInformation(CommandContext.Trace2);
            log.AppendLine($"OSType: {platformInfo.OperatingSystemType}");
            log.AppendLine($"OSVersion: {platformInfo.OperatingSystemVersion}");

            log.Append("Reading environment variables...");
            IDictionary envars = Environment.GetEnvironmentVariables();
            log.AppendLine(" OK");

            log.AppendLine(" Variables:");
            foreach (DictionaryEntry envar in envars)
            {
                log.AppendFormat("{0}={1}", envar.Key, envar.Value);
                log.AppendLine();
            }
            log.AppendLine();

            return Task.FromResult(true);
        }
    }
}
