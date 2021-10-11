using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager.Diagnostics
{
    public class GitDiagnostic : Diagnostic
    {
        private readonly IGit _git;

        public GitDiagnostic(IGit git)
            : base("Git")
        {
            EnsureArgument.NotNull(git, nameof(git));

            _git = git;
        }

        protected override Task<bool> RunInternalAsync(StringBuilder log, IList<string> additionalFiles)
        {
            log.Append("Getting Git version...");
            GitVersion gitVersion =  _git.Version;
            log.AppendLine(" OK");
            log.AppendLine($"Git version is '{gitVersion.OriginalString}'");

            log.Append("Locating current repository...");
            string thisRepo =_git.GetCurrentRepository();
            log.AppendLine(" OK");
            log.AppendLine(thisRepo is null ? "Not inside a Git repository." : $"Git repository at '{thisRepo}'");

            log.Append("Listing all Git configuration...");
            Process configProc = _git.CreateProcess("config --list --show-origin");
            configProc.Start();
            configProc.WaitForExit();
            string gitConfig = configProc.StandardOutput.ReadToEnd().TrimEnd();
            log.AppendLine(" OK");
            log.AppendLine("Git configuration:");
            log.AppendLine(gitConfig);
            log.AppendLine();

            return Task.FromResult(true);
        }
    }
}
