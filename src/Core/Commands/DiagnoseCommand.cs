using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GitCredentialManager.Diagnostics;
using Spectre.Console;

namespace GitCredentialManager.Commands
{
    public class DiagnoseCommand : Command
    {
        private readonly ICommandContext _context;
        private readonly IAnsiConsole _ansiConsole;
        private readonly List<IDiagnostic> _diagnostics = new();

        public DiagnoseCommand(ICommandContext context) : this(context, AnsiConsole.Console) { }

        internal DiagnoseCommand(ICommandContext context, IAnsiConsole ansiConsole)
            : base("diagnose", "Run diagnostics and gather logs to diagnose problems with Git Credential Manager")
        {
            EnsureArgument.NotNull(context, nameof(context));

            _context = context;
            _ansiConsole = ansiConsole;

            var output = new Option<string>(["--output", "-o"], "Output directory for diagnostic logs.");
            AddOption(output);

            var strict = new Option<bool>(["--strict"], "Exit with a non-zero code if diagnostic warnings are present.");
            AddOption(strict);

            this.SetHandler(ExecuteAsync, output, strict);
        }

        public void AddStandardDiagnostics()
        {
            _diagnostics.Add(new EnvironmentDiagnostic(_context));
            _diagnostics.Add(new FileSystemDiagnostic(_context));
            _diagnostics.Add(new NetworkingDiagnostic(_context));
            _diagnostics.Add(new GitDiagnostic(_context));
            _diagnostics.Add(new CredentialStoreDiagnostic(_context));
            _diagnostics.Add(new EntraAuthenticationDiagnostic(_context));
        }

        public void AddDiagnostic(IDiagnostic diagnostic)
        {
            _diagnostics.Add(diagnostic);
        }

        public void AddDiagnostics(IEnumerable<IDiagnostic> diagnostics)
        {
            _diagnostics.AddRange(diagnostics);
        }

        private async Task<int> ExecuteAsync(string output, bool strict)
        {
            using var _ = Trace2.StartRegion("diag_cmd", "run");

            // Don't use IStandardStreams or IConsoleService for writing output in this command
            // as we cannot trust any component on the ICommandContext is working correctly.
            // Using the default AnsiConsole directly should be safe.
            _ansiConsole.MarkupLine("[b]Running diagnostics...[/]");
            _ansiConsole.WriteLine();

            if (_diagnostics.Count == 0)
            {
                _ansiConsole.WriteLine("No diagnostics to run.");
                return 0;
            }

            string currentDir = Directory.GetCurrentDirectory();
            string outputDir;
            if (string.IsNullOrWhiteSpace(output))
            {
                outputDir = currentDir;
            }
            else
            {
                if (!Directory.Exists(output))
                {
                    Directory.CreateDirectory(output);
                }

                outputDir = Path.GetFullPath(Path.Combine(currentDir, output));
            }

            string logFilePath = Path.Combine(outputDir, "gcm-diagnose.log");
            var results = new List<DiagnosticResult>();

            using var fullLog = new StreamWriter(logFilePath, append: false, Encoding.UTF8);
            WriteLogHeader(fullLog);

            await AnsiConsole.Status()
                .Spinner(Spinner.Known.BouncingBar)
                .StartAsync("Running", async ctx =>
            {
                foreach (IDiagnostic diagnostic in _diagnostics)
                {
                    DiagnosticResult result = await RunDiagnosticAsync(diagnostic, fullLog, ctx);
                    results.Add(result);
                }
            });

            IReadOnlyList<string> additionalFiles = CopyFiles(outputDir, results.SelectMany(x => x.AdditionalFiles));

            _ansiConsole.WriteLine();
            PrintSummary(logFilePath, results, additionalFiles);

            fullLog.Close();

            // In strict mode we treat warnings as errors, otherwise we only treat errors as failures.
            DiagnosticOutcome[] failureOutcomes = strict
                ? [DiagnosticOutcome.Error, DiagnosticOutcome.Warning]
                : [DiagnosticOutcome.Error];
            return results.Any(x => failureOutcomes.Contains(x.Outcome)) ? 1 : 0;
        }

        private void PrintSummary(string logFilePath, IReadOnlyList<DiagnosticResult> results, IReadOnlyList<string> additionalFiles)
        {
            int numPassed = results.Count(x => x.Outcome == DiagnosticOutcome.Success);
            int numFailed = results.Count(x => x.Outcome == DiagnosticOutcome.Error);
            int numWarned = results.Count(x => x.Outcome == DiagnosticOutcome.Warning);
            int numSkipped = results.Count(x => x.Outcome == DiagnosticOutcome.Skipped);

            _ansiConsole.MarkupLine("[b u]Summary[/]");

            void WriteCount(int count, string label, string color = null)
            {
                if (color is not null && count > 0)
                {
                    _ansiConsole.Markup($"[{color}][b]{Markup.Escape(count.ToString())}[/] {Markup.Escape(label)}[/]");
                }
                else
                {
                    _ansiConsole.MarkupInterpolated($"[b]{count}[/] {Markup.Escape(label)}");
                }
            }

            const string sep = "    ";
            WriteCount(numPassed, "passed", "green");
            _ansiConsole.Write(sep);
            WriteCount(numSkipped, "skipped");
            _ansiConsole.Write(sep);
            WriteCount(numWarned, "warned", "yellow");
            _ansiConsole.Write(sep);
            WriteCount(numFailed, "failed", "red");
            _ansiConsole.WriteLine();

            _ansiConsole.WriteLine();
            _ansiConsole.MarkupLine("[b u]Log files[/]");
            _ansiConsole.WriteLine(logFilePath);
            foreach (string filePath in additionalFiles)
            {
                _ansiConsole.WriteLine(filePath);
            }
            _ansiConsole.WriteLine();
            _ansiConsole.MarkupLine("[yellow]Caution: Log files may include [b]sensitive information[/] - redact before sharing![/]");
            _ansiConsole.WriteLine();

            if (numFailed + numWarned > 0)
            {
                _ansiConsole.MarkupLine("[yellow]Diagnostics indicate a possible problem with your installation.[/]");
                _ansiConsole.MarkupLine($"[yellow]Please open an issue at [link]{Constants.HelpUrls.GcmNewIssue}[/] and include log files.[/]");
                _ansiConsole.WriteLine();
            }
        }

        private void WriteLogHeader(StreamWriter fullLog)
        {
            fullLog.WriteLine("Diagnose log at {0:s}Z", DateTime.UtcNow);
            fullLog.WriteLine();
            fullLog.WriteLine($"AppPath: {_context.ApplicationPath}");
            fullLog.WriteLine($"InstallDir: {_context.InstallationDirectory}");
            fullLog.WriteLine(
                AssemblyUtils.TryGetAssemblyVersion(out string version)
                    ? $"Version: {version}"
                    : "Version: [!] Failed to get version information [!]"
            );
            fullLog.WriteLine();
        }

        private async Task<DiagnosticResult> RunDiagnosticAsync(
            IDiagnostic diagnostic, StreamWriter fullLog, StatusContext statusContext)
        {
            fullLog.WriteLine("------------");
            fullLog.WriteLine($"Diagnostic: {diagnostic.Name}");

            if (!diagnostic.CanRun(out string skipReason))
            {
                fullLog.Write("Outcome: Skipped");
                if (!string.IsNullOrWhiteSpace(skipReason))
                {
                    fullLog.Write($" ({skipReason})");
                }
                fullLog.WriteLine();

                _ansiConsole.MarkupLineInterpolated($"[grey b][[SKIP]][/] {diagnostic.Name} [grey i]({skipReason})[/]");

                return DiagnosticResult.Skipped(skipReason);
            }

            statusContext.Status(diagnostic.Name);
            DiagnosticResult result = await diagnostic.RunAsync();

            fullLog.WriteLine("Outcome: {0}", result.Outcome);

            WriteException(fullLog, result.Exception);

            fullLog.WriteLine("Log:");
            foreach (var report in result.Reports)
            {
                fullLog.WriteLine(report.Message);
            }

            switch (result.Outcome)
            {
                case DiagnosticOutcome.Success:
                    _ansiConsole.MarkupLineInterpolated($"[green b][[ OK ]][/] {diagnostic.Name}");
                    break;
                case DiagnosticOutcome.Warning:
                    _ansiConsole.MarkupLineInterpolated($"[yellow b][[WARN]][/] {diagnostic.Name}");
                    break;
                case DiagnosticOutcome.Error:
                    _ansiConsole.MarkupLineInterpolated($"[red b][[FAIL]][/] {diagnostic.Name}");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (result.Outcome != DiagnosticOutcome.Success)
            {
                if (result.Exception is not null)
                {
                    _ansiConsole.WriteLine();
                    _ansiConsole.MarkupLine("[red u]Exception Details[/]");
                    _ansiConsole.WriteLine(result.Exception.ToString());
                }

                _ansiConsole.WriteLine();
                _ansiConsole.MarkupLine("[u]Diagnostic Log[/]");
                foreach (var report in result.Reports)
                {
                    _ansiConsole.WriteLine(report.Message);
                }

                _ansiConsole.WriteLine();
            }

            fullLog.Flush();
            return result;
        }

        private IReadOnlyList<string> CopyFiles(string outputDir, IEnumerable<string> additionalFiles)
        {
            var extraLogs = new List<string>();
            foreach (string filePath in additionalFiles)
            {
                string fileName = Path.GetFileName(filePath);
                string destPath = Path.Combine(outputDir, fileName);
                try
                {
                    File.Copy(filePath, destPath, overwrite: true);
                }
                catch
                {
                    _ansiConsole.MarkupLineInterpolated($"[red]Failed to copy additional file '{filePath}'[/]");
                }

                extraLogs.Add(destPath);
            }

            return extraLogs;
        }

        private void WriteException(StreamWriter log, Exception exception)
        {
            if (exception is null)
            {
                return;
            }

            if (exception is AggregateException aex)
            {
                log.WriteLine("Exception: AggregateException");
                log.WriteLine("InnerExceptions (flattened):");
                foreach (var inner in aex.Flatten().InnerExceptions)
                {
                    log.WriteLine(inner.ToString());
                }
            }
            else
            {
                log.WriteLine("Exception: {0}", exception);
            }
        }
    }
}
