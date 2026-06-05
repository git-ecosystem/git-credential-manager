using System;
using System.Collections.Generic;

namespace GitCredentialManager;

/// <summary>
/// Represents the response from a host provider to a Git credential helper
/// invocation (typically a <c>get</c>).
/// </summary>
/// <remarks>
/// <para>
/// Holds the resolved <see cref="ICredential"/> together with any non-credential
/// output the provider wants to emit back to Git. Strongly-typed properties for
/// newer Git credential protocol fields (e.g. <c>state[]</c>, <c>continue</c>,
/// <c>authtype</c>, <c>credential</c>, <c>ephemeral</c>, <c>password_expiry_utc</c>,
/// <c>oauth_refresh_token</c>) will be added here as the corresponding
/// capabilities are wired up; see <see cref="GitCapabilities"/>.
/// </para>
/// <para>
/// Until then, <see cref="AdditionalProperties"/> remains the escape hatch for
/// arbitrary extra output keys (for example, the generic provider's
/// <c>ntlm=allow</c> hint).
/// </para>
/// </remarks>
public class GitResponse
{
    public GitResponse(ICredential credential)
    {
        Credential = credential;
    }

    /// <summary>
    /// The credential resolved or generated for the request.
    /// </summary>
    public ICredential Credential { get; }

    /// <summary>
    /// Additional, untyped output to be emitted alongside the credential.
    /// </summary>
    /// <remarks>
    /// This is the legacy escape hatch for non-credential output. New code
    /// should prefer typed properties on <see cref="GitResponse"/> as they
    /// are added.
    /// </remarks>
    public IDictionary<string, string> AdditionalProperties { get; set; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
