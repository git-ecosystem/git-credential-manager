using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GitCredentialManager.Diagnostics
{
    public class EnvironmentDiagnostic : Diagnostic
    {
        private readonly IEnvironment _env;

        public EnvironmentDiagnostic(IEnvironment env)
            : base("Environment")
        {
            EnsureArgument.NotNull(env, nameof(env));

            _env = env;
        }

        protected override Task<bool> RunInternalAsync(StringBuilder log, IList<string> additionalFiles)
        {
            PlatformInformation platformInfo = PlatformUtils.GetPlatformInformation();
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
