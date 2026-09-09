using System;
using System.Threading.Tasks;
using GitCredentialManager;
using GitCredentialManager.Diagnostics;

namespace GitHub.Diagnostics
{
    public class GitHubApiDiagnostic : Diagnostic
    {
        private readonly IGitHubRestApi _api;

        public GitHubApiDiagnostic(IGitHubRestApi api, ICommandContext commandContext)
            : base("GitHub API", commandContext)
        {
            _api = api;
        }

        protected override async Task RunInternalAsync(IDiagnosticReporter reporter)
        {
            var targetUri = new Uri("https://github.com");
            reporter.ReportInfo($"Using '{targetUri}' as API target.");

            reporter.ReportProgress("Querying '/meta' endpoint");
            GitHubMetaInfo metaInfo = await _api.GetMetaInfoAsync(targetUri);
        }
    }
}
