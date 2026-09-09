using System;
using System.IO;
using System.Threading.Tasks;

namespace GitCredentialManager.Diagnostics
{
    public class FileSystemDiagnostic : Diagnostic
    {
        public FileSystemDiagnostic(ICommandContext commandContext)
            : base("File system", commandContext)
        {  }

        protected override Task RunInternalAsync(IDiagnosticReporter reporter)
        {
            string tempDir = Path.GetTempPath();
            reporter.ReportInfo($"Temporary directory is '{tempDir}'");

            reporter.ReportProgress("Checking basic file I/O");
            const string testContent = "Hello, GCM!";

            string fileName = Guid.NewGuid().ToString("N").Substring(8);
            string path = Path.Combine(tempDir, fileName);
            reporter.ReportProgress($"Writing to temporary file '{path}'");
            File.WriteAllText(path, testContent);

            reporter.ReportProgress($"Reading from temporary file '{path}'");
            string actualContent = File.ReadAllText(path);

            if (!StringComparer.Ordinal.Equals(testContent, actualContent))
            {
                reporter.ReportError("File data did not match!");
                reporter.ReportError($"Expected: {testContent}");
                reporter.ReportError($"Actual: {actualContent}");
                return Task.CompletedTask;
            }

            reporter.ReportProgress($"Deleting temporary file '{path}'");
            File.Delete(path);

            reporter.ReportProgress("Testing IFileSystem instance");
            reporter.ReportInfo($"UserHomePath: {Context.FileSystem.UserHomePath}");
            reporter.ReportInfo($"UserDataDirectoryPath: {Context.FileSystem.UserDataDirectoryPath}");
            reporter.ReportInfo($"GetCurrentDirectory(): {Context.FileSystem.GetCurrentDirectory()}");

            return Task.CompletedTask;
        }
    }
}
