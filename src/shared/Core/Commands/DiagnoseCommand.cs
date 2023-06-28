using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GitCredentialManager.Diagnostics;

namespace GitCredentialManager.Commands
{
    public class DiagnoseCommand : Command
    {
        private const string TestOutputIndent = "    ";

        private readonly ICommandContext _context;
        private readonly ICollection<IDiagnostic> _diagnostics;

        public DiagnoseCommand(ICommandContext context)
            : base("diagnose", "Run diagnostics and gather logs to diagnose problems with Git Credential Manager")
        {
            EnsureArgument.NotNull(context, nameof(context));

            _context = context;
            _diagnostics = new List<IDiagnostic>
            {
                // Add standard diagnostics
                new EnvironmentDiagnostic(context),
                new FileSystemDiagnostic(context),
                new NetworkingDiagnostic(context),
                new GitDiagnostic(context),
                new CredentialStoreDiagnostic(context),
                new MicrosoftAuthenticationDiagnostic(context)
            };

            var output = new Option<string>(new[] { "--output", "-o" }, "Output directory for diagnostic logs.");
            AddOption(output);

            this.SetHandler(ExecuteAsync, output);
        }

        public void AddDiagnostic(IDiagnostic diagnostic)
        {
            _diagnostics.Add(diagnostic);
        }

        private async Task<int> ExecuteAsync(string output)
        {
            // Don't use IStandardStreams for writing output in this command as we
            // cannot trust any component on the ICommandContext is working correctly.
            Console.WriteLine($"Running diagnostics...{Environment.NewLine}");

            if (_diagnostics.Count == 0)
            {
                Console.WriteLine("No diagnostics to run.");
                return 0;
            }

            int numFailed = 0;
            int numSkipped = 0;

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
            var extraLogs = new List<string>();

            using var fullLog = new StreamWriter(logFilePath, append: false, Encoding.UTF8);
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

            foreach (IDiagnostic diagnostic in _diagnostics)
            {
                fullLog.WriteLine("------------");
                fullLog.WriteLine($"Diagnostic: {diagnostic.Name}");

                if (!diagnostic.CanRun())
                {
                    fullLog.WriteLine("Skipped: True");
                    fullLog.WriteLine();

                    Console.Write(" ");
                    ConsoleEx.WriteColor("[SKIP]", ConsoleColor.Gray);
                    Console.WriteLine(" {0}", diagnostic.Name);

                    numSkipped++;
                    continue;
                }

                string inProgressMsg = $"  >>>>  {diagnostic.Name}";
                Console.Write(inProgressMsg);

                fullLog.WriteLine("Skipped: False");
                DiagnosticResult result = await diagnostic.RunAsync();
                fullLog.WriteLine("Success: {0}", result.IsSuccess);

                if (result.Exception is null)
                {
                    fullLog.WriteLine("Exception: None");
                }
                else
                {
                    fullLog.WriteLine("Exception:");
                    fullLog.WriteLine(result.Exception.ToString());
                }

                fullLog.WriteLine("Log:");
                fullLog.WriteLine(result.DiagnosticLog);

                Console.Write(new string('\b', inProgressMsg.Length - 1));
                ConsoleEx.WriteColor(
                    result.IsSuccess ? "[ OK ]" : "[FAIL]",
                    result.IsSuccess ? ConsoleColor.DarkGreen : ConsoleColor.Red
                );
                Console.WriteLine(" {0}", diagnostic.Name);

                if (!result.IsSuccess)
                {
                    numFailed++;

                    if (result.Exception is not null)
                    {
                        Console.WriteLine();
                        ConsoleEx.WriteLineIndent("[!] Encountered an exception [!]");
                        ConsoleEx.WriteLineIndent(result.Exception.ToString());
                    }

                    Console.WriteLine();
                    ConsoleEx.WriteLineIndent("[*] Diagnostic test log [*]");
                    ConsoleEx.WriteLineIndent(result.DiagnosticLog);

                    Console.WriteLine();
                }

                foreach (string filePath in result.AdditionalFiles)
                {
                    string fileName = Path.GetFileName(filePath);
                    string destPath = Path.Combine(outputDir, fileName);
                    try
                    {
                        File.Copy(filePath, destPath, overwrite: true);
                    }
                    catch
                    {
                        ConsoleEx.WriteLineIndent($"Failed to copy additional file '{filePath}'");
                    }

                    extraLogs.Add(destPath);
                }

                fullLog.Flush();
            }

            Console.WriteLine();
            string summary = $"Diagnostic summary: {_diagnostics.Count - numFailed} passed, {numSkipped} skipped, {numFailed} failed.";
            Console.WriteLine(summary);
            Console.WriteLine("Log files:");
            Console.WriteLine($"  {logFilePath}");
            foreach (string log in extraLogs)
            {
                Console.WriteLine($"  {log}");
            }
            Console.WriteLine();
            Console.WriteLine("Caution: Log files may include sensitive information - redact before sharing.");
            Console.WriteLine();

            if (numFailed > 0)
            {
                Console.WriteLine("Diagnostics indicate a possible problem with your installation.");
                Console.WriteLine($"Please open an issue at {Constants.HelpUrls.GcmNewIssue} and include log files.");
                Console.WriteLine();
            }

            fullLog.Close();
            return numFailed;
        }

        private static class ConsoleEx
        {
            public static void WriteLineIndent(string str)
            {
                string[] lines = str?.Split('\n', '\r');

                if (lines is null) return;

                foreach (string line in lines)
                {
                    Console.Write(TestOutputIndent);
                    Console.WriteLine(line);
                }
            }

            public static  void WriteColor(string str, ConsoleColor fgColor)
            {
                var initFgColor = Console.ForegroundColor;
                Console.ForegroundColor = fgColor;
                Console.Write(str);
                Console.ForegroundColor = initFgColor;
            }
        }
    }
}
