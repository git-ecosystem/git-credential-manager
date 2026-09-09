using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitCredentialManager.Diagnostics;

public interface IDiagnostic
{
    string Name { get; }

    bool CanRun(out string reason);

    Task<DiagnosticResult> RunAsync(Action<string> progress = null);
}

public enum DiagnosticOutcome
{
    Success,
    Warning,
    Error,
    Skipped
}

public enum DiagnosticReportKind
{
    Progress,
    Information,
    Warning,
    Error
}

public sealed class DiagnosticReport(DiagnosticReportKind kind, string message, Exception exception = null)
{
    public DiagnosticReportKind Kind { get; } = kind;
    public string Message { get; } = message;
    public Exception Exception { get; } = exception;

    public static DiagnosticReport Info(string message) => new(DiagnosticReportKind.Information, message);
    public static DiagnosticReport Warning(string message) => new(DiagnosticReportKind.Warning, message);
    public static DiagnosticReport Error(string message, Exception exception = null) => new(DiagnosticReportKind.Error, message, exception);
    public static DiagnosticReport Progress(string message) => new(DiagnosticReportKind.Progress, message);
}

public interface IDiagnosticReporter
{
    void ReportProgress(string message);
    void ReportInfo(string message);
    void ReportWarning(string message);
    void ReportError(string message, Exception exception = null);
    void AddFile(string path);
}

public abstract class Diagnostic(string name, ICommandContext context) : IDiagnostic
{
    protected readonly ICommandContext Context = context;

    public string Name { get; } = name;

    public virtual bool CanRun(out string reason)
    {
        reason = null;
        return true;
    }

    public async Task<DiagnosticResult> RunAsync(Action<string> progress)
    {
        var reporter = new DiagnosticReporter(progress);

        try
        {
            if (CanRun(out string skipReason))
            {
                await RunInternalAsync(reporter);
            }
            else
            {
                return DiagnosticResult.Skipped(skipReason);
            }
        }
        catch (Exception ex)
        {
            reporter.ReportError($"Unhandled exception: {ex.Message}", ex);
        }

        return reporter.CreateResult();
    }

    protected abstract Task RunInternalAsync(IDiagnosticReporter reporter);

    private class DiagnosticReporter(Action<string> progress) : IDiagnosticReporter
    {
        private readonly Action<string> _progress = progress;
        private readonly List<DiagnosticReport> _reports = new();
        private readonly List<string> _files = new();

        public void AddFile(string path) => _files.Add(path);

        public void ReportProgress(string message)
        {
            _reports.Add(DiagnosticReport.Progress(message));
            _progress?.Invoke(message);
        }

        public void ReportInfo(string message) => _reports.Add(DiagnosticReport.Info(message));
        public void ReportWarning(string message) => _reports.Add(DiagnosticReport.Warning(message));
        public void ReportError(string message, Exception exception = null) =>
            _reports.Add(DiagnosticReport.Error(message, exception));

        public DiagnosticResult CreateResult() => new(_reports, _files);
    }
}

public class DiagnosticResult
{
    public static DiagnosticResult Skipped(string reason) => new([], [], reason);

    public DiagnosticResult(IEnumerable<DiagnosticReport> reports, IEnumerable<string> additionalFiles)
        : this(reports, additionalFiles, null) { }

    private DiagnosticResult(IEnumerable<DiagnosticReport> reports, IEnumerable<string> additionalFiles, string skipReason)
    {
        Reports = reports.ToArray();
        AdditionalFiles = additionalFiles.ToArray();
        SkipReason = skipReason;

        ErrorCount = Reports.Count(x => x.Kind == DiagnosticReportKind.Error);
        WarningCount = Reports.Count(x => x.Kind == DiagnosticReportKind.Warning);
        Outcome = !string.IsNullOrWhiteSpace(skipReason)
            ? DiagnosticOutcome.Skipped
            : ErrorCount > 0
                ? DiagnosticOutcome.Error
                : WarningCount > 0
                    ? DiagnosticOutcome.Warning
                    : DiagnosticOutcome.Success;

        Exception[] exceptions = Reports.Where(x => x.Exception is not null)
            .Select(x => x.Exception)
            .ToArray();

        Exception = exceptions.Length switch
        {
            0 => null,
            1 => exceptions[0],
            _ => new AggregateException(exceptions)
        };
    }

    public DiagnosticOutcome Outcome { get; }
    public string SkipReason { get; }
    public int ErrorCount { get; }
    public int WarningCount { get; }
    public IReadOnlyList<DiagnosticReport> Reports { get; }
    public IReadOnlyList<string> AdditionalFiles { get; }
    public Exception Exception { get; }
}
