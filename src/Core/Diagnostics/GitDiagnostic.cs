using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace GitCredentialManager.Diagnostics
{
    public class GitDiagnostic : Diagnostic
    {
        public GitDiagnostic(ICommandContext commandContext)
            : base("Git", commandContext)
        { }

        protected override Task<bool> RunInternalAsync(StringBuilder log, IList<string> additionalFiles)
        {
            log.Append("Getting Git version...");
            GitVersion gitVersion = CommandContext.Git.Version;
            log.AppendLine(" OK");
            log.AppendLine($"Git version is '{gitVersion.OriginalString}'");

            log.Append("Locating current repository...");
            if (!CommandContext.Git.IsInsideRepository())
            {
                log.AppendLine("Not inside a Git repository.");
            }
            else
            {
                string thisRepo = CommandContext.Git.GetCurrentRepository();
                log.AppendLine($"Git repository at '{thisRepo}'");
            }
            log.AppendLine(" OK");

            log.Append("Listing all Git configuration...");
            ChildProcess configProc = CommandContext.Git.CreateProcess("config --list --show-origin");
            configProc.Start(Trace2ProcessClass.Git);
            // To avoid deadlocks, always read the output stream first and then wait
            // TODO: don't read in all the data at once; stream it
            string gitConfig = configProc.StandardOutput.ReadToEnd().TrimEnd();
            configProc.WaitForExit();
            log.AppendLine(" OK");
            log.AppendLine("Git configuration:");
            log.AppendLine(gitConfig);
            log.AppendLine();

            return Task.FromResult(true);
        }
    }
}
