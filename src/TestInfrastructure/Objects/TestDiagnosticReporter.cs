using System;
using System.Collections.Generic;
using GitCredentialManager.Diagnostics;

namespace GitCredentialManager.Tests.Objects;

public class TestDiagnosticReporter : IDiagnosticReporter
{
    public IList<string> Progress { get; } = new List<string>();
    public IList<string> Info { get; } = new List<string>();
    public IList<string> Warnings { get; } = new List<string>();
    public IList<(string Message, Exception Exception)> Errors { get; } = new List<(string, Exception)>();
    public IList<string> Files { get; } = new List<string>();

    public void ReportProgress(string message)
    {
        Progress.Add(message);
    }

    public void ReportInfo(string message)
    {
        Info.Add(message);
    }

    public void ReportWarning(string message)
    {
        Warnings.Add(message);
    }

    public void ReportError(string message, Exception exception = null)
    {
        Errors.Add((message, exception));
    }

    public void AddFile(string path)
    {
        Files.Add(path);
    }
}
