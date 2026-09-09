using System;
using System.Threading.Tasks;

namespace GitCredentialManager.Diagnostics
{
    public class CredentialStoreDiagnostic : Diagnostic
    {
        public CredentialStoreDiagnostic(ICommandContext commandContext)
            : base("Credential storage", commandContext)
        { }

        protected override Task RunInternalAsync(IDiagnosticReporter reporter)
        {
            reporter.ReportInfo($"Credential store is: {Context.CredentialStore.Name}");

            // Create a service that is guaranteed to be unique
            string service = $"https://example.com/{Guid.NewGuid():N}";
            const string account = "john.doe";
            const string password = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]

            try
            {
                reporter.ReportProgress("Writing test credential...");
                Context.CredentialStore.AddOrUpdate(service, account, password);

                reporter.ReportProgress("Reading test credential...");
                ICredential outCredential = Context.CredentialStore.Get(service, account);
                if (outCredential is null)
                {
                    reporter.ReportError("Test credential object is null!");
                    return Task.CompletedTask;
                }

                if (!StringComparer.Ordinal.Equals(account, outCredential.Account))
                {
                    reporter.ReportError("Test credential account did not match!");
                    reporter.ReportError($"Expected: {account}");
                    reporter.ReportError($"Actual: {outCredential.Account}");
                    return Task.CompletedTask;
                }

                if (!StringComparer.Ordinal.Equals(password, outCredential.Password))
                {
                    reporter.ReportError("Test credential password did not match!");
                    reporter.ReportError($"Expected: {password}");
                    reporter.ReportError($"Actual: {outCredential.Password}");
                    return Task.CompletedTask;
                }
            }
            finally
            {
                reporter.ReportProgress("Deleting test credential");
                Context.CredentialStore.Remove(service, account);
            }

            return Task.CompletedTask;
        }
    }
}
