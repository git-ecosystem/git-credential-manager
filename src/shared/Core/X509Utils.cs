using System.Security.Cryptography.X509Certificates;

namespace GitCredentialManager;

public static class X509Utils
{
    public static X509Certificate2 GetCertificateByThumbprint(string thumbprint)
    {
        foreach (var location in new[]{StoreLocation.CurrentUser, StoreLocation.LocalMachine})
        {
            using var store = new X509Store(StoreName.My, location);
            store.Open(OpenFlags.ReadOnly);

            X509Certificate2Collection certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
            if (certs.Count > 0)
            {
                return certs[0];
            }
        }

        return null;
    }
}
