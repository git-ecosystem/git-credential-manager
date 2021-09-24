using System.Collections.Generic;

namespace Microsoft.Git.CredentialManager.Diagnostics
{
    public interface IDiagnosticProvider
    {
        IEnumerable<IDiagnostic> GetDiagnostics();
    }
}
