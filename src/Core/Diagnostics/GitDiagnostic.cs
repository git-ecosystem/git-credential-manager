using System.Threading.Tasks;

namespace GitCredentialManager.Diagnostics
{
    public class GitDiagnostic : Diagnostic
    {
        public GitDiagnostic(ICommandContext commandContext)
            : base("Git", commandContext)
        { }

        protected override Task RunInternalAsync(IDiagnosticReporter reporter)
        {
            reporter.ReportProgress("Getting Git version");
            GitVersion gitVersion = Context.Git.Version;
            reporter.ReportInfo($"Git version is '{gitVersion.OriginalString}'");

            reporter.ReportProgress("Locating current repository");
            if (!Context.Git.IsInsideRepository())
            {
                reporter.ReportInfo("Not inside a Git repository.");
            }
            else
            {
                string thisRepo = Context.Git.GetCurrentRepository();
                reporter.ReportInfo($"Git repository at '{thisRepo}'");
            }

            reporter.ReportProgress("Listing all Git configuration");
            ChildProcess configProc = Context.Git.CreateProcess("config --list --show-origin");
            configProc.Start(Trace2ProcessClass.Git);
            // To avoid deadlocks, always read the output stream first and then wait
            // TODO: don't read in all the data at once; stream it
            string gitConfig = configProc.StandardOutput.ReadToEnd().TrimEnd();
            configProc.WaitForExit();
            reporter.ReportInfo("Git configuration:");
            reporter.ReportInfo(gitConfig);

            return Task.CompletedTask;
        }
    }
}
