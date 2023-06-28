using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GitCredentialManager.Diagnostics
{
    public interface IDiagnostic
    {
        string Name { get; }

        bool CanRun();

        Task<DiagnosticResult> RunAsync();
    }

    public abstract class Diagnostic : IDiagnostic
    {
        protected ICommandContext CommandContext;

        protected Diagnostic(string name, ICommandContext commandContext)
        {
            Name = name;
            CommandContext = commandContext;
        }

        public string Name { get; }

        public virtual bool CanRun()
        {
            return true;
        }

        public async Task<DiagnosticResult> RunAsync()
        {
            var log = new StringBuilder();

            bool success = false;
            Exception exception = null;
            var additionalFiles = new List<string>();
            try
            {
                success = await RunInternalAsync(log, additionalFiles);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            return new DiagnosticResult
            {
                IsSuccess = success,
                DiagnosticLog = log.ToString(),
                Exception = exception,
                AdditionalFiles = additionalFiles
            };
        }

        protected abstract Task<bool> RunInternalAsync(StringBuilder log, IList<string> additionalFiles);
    }

    public class DiagnosticResult
    {
        public bool IsSuccess { get; set; }
        public Exception Exception { get; set; }
        public string DiagnosticLog { get; set; }
        public ICollection<string> AdditionalFiles { get; set; }
    }
}
