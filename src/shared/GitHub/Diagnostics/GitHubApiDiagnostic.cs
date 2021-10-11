using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager.Diagnostics;

namespace GitHub.Diagnostics
{
    public class GitHubApiDiagnostic : Diagnostic
    {
        private readonly IGitHubRestApi _api;

        public GitHubApiDiagnostic(IGitHubRestApi api)
            : base("GitHub API")
        {
            _api = api;
        }

        protected override async Task<bool> RunInternalAsync(StringBuilder log, IList<string> additionalFiles)
        {
            var targetUri = new Uri("https://github.com");
            log.AppendLine($"Using '{targetUri}' as API target.");

            log.Append("Querying '/meta' endpoint...");
            GitHubMetaInfo metaInfo = await _api.GetMetaInfoAsync(targetUri);
            log.AppendLine(" OK");

            return true;
        }
    }
}
