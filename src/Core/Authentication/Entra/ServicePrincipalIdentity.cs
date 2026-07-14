using System.Security.Cryptography.X509Certificates;

namespace GitCredentialManager.Authentication.Entra;

public class ServicePrincipalIdentity
{
    /// <summary>
    /// Client ID of the service principal.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Tenant ID of the service principal.
    /// </summary>
    public string TenantId { get; set; }

    /// <summary>
    /// Certificate used to authenticate the service principal.
    /// </summary>
    /// <remarks>
    /// If both <see cref="Certificate"/> and <see cref="ClientSecret"/> are set, the certificate will be used.
    /// </remarks>
    public X509Certificate2 Certificate { get; set; }

    /// <summary>
    /// Secret used to authenticate the service principal.
    /// </summary>
    /// <remarks>
    /// If both <see cref="Certificate"/> and <see cref="ClientSecret"/> are set, the certificate will be used.
    /// </remarks>
    public string ClientSecret { get; set; }

    /// <summary>
    /// Whether the authentication should send X5C
    /// </summary>
    public bool SendX5C { get; set; }
}
