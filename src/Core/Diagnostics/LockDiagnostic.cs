using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GitCredentialManager.Concurrency;

namespace GitCredentialManager.Diagnostics;

public class LockDiagnostic(ICommandContext context) : Diagnostic("Locking", context)
{
    protected override Task RunInternalAsync(IDiagnosticReporter reporter)
    {
        reporter.ReportProgress("Checking for orphaned lock files...");

        string lockDir = Path.Join(Context.FileSystem.UserDataDirectoryPath, Constants.LocksDirectoryName);
        List<string> lockFiles = Context.FileSystem.EnumerateFiles(lockDir, "*").ToList();

        var orphans = new List<string>();
        reporter.ReportInfo($"Found {lockFiles.Count} lock files in '{lockDir}':");
        foreach (string path in lockFiles)
        {
            string name = Path.GetFileName(path);
            var lockFile = new FileLock(Context.FileSystem, path);
            if (lockFile.TryGetInfo(out int pid, out DateTime createdAt))
            {
                try
                {
                    DateTime startTime = Context.ProcessManager.GetStartTimeUtc(pid);
                    if (startTime > createdAt)
                    {
                        orphans.Add(path);
                        reporter.ReportWarning($"[ORPHAN] {name} (created by {pid} at {createdAt:O}; PID is reused)");
                    }
                    else
                    {
                        reporter.ReportInfo($"[ACTIVE] {name} (owned by {pid})");
                    }
                }
                catch
                {
                    orphans.Add(path);
                    reporter.ReportWarning($"[ORPHAN] {name} (created by {pid} at {createdAt:O})");
                }
            }
            else
            {
                orphans.Add(path);
                reporter.ReportError($"[UNKNWN] {name} (could not be inspected)");
            }
        }

        if (orphans.Count > 0)
        {
            reporter.ReportInfo(
                $"Found {orphans.Count} orphaned/corrupt lock files. " +
                $"To remove them, delete the following files:");
            foreach (string path in orphans)
            {
                reporter.ReportInfo($" - {path}");
            }
        }

        return Task.CompletedTask;
    }
}
