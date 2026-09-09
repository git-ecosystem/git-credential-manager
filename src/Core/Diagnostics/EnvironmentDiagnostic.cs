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

        protected override Task RunInternalAsync(IDiagnosticReporter reporter)
        {
            PlatformInformation platformInfo = PlatformUtils.GetPlatformInformation(Context.Trace2);
            reporter.ReportInfo($"OSType: {platformInfo.OperatingSystemType}");
            reporter.ReportInfo($"OSVersion: {platformInfo.OperatingSystemVersion}");

            reporter.ReportProgress("Reading environment variables");
            IDictionary envars = Environment.GetEnvironmentVariables();

            reporter.ReportInfo("Variables:");
            foreach (DictionaryEntry envar in envars)
            {
                reporter.ReportInfo($"{envar.Key}={envar.Value}");
            }

            return Task.CompletedTask;
        }
    }
}
