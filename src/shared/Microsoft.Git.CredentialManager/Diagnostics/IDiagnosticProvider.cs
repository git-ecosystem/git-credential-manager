using System.Collections.Generic;

namespace GitCredentialManager.Diagnostics
{
    public interface IDiagnosticProvider
    {
        IEnumerable<IDiagnostic> GetDiagnostics();
    }
}
