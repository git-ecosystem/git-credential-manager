using System;
using System.Collections.Generic;

namespace GitCredentialManager;

/// <summary>
/// Represents the response from a host provider to a Git credential helper
/// invocation (typically a <c>get</c>).
/// </summary>
/// <remarks>
/// <para>
/// A successful response holds the resolved <see cref="ICredential"/> together
/// with any non-credential output the provider wants to emit back to Git
/// (see <see cref="AdditionalProperties"/>).
/// </para>
/// <para>
/// A response can also be a <em>cancellation</em>: the provider was asked for a
/// credential but deliberately declined to produce one (the user closed an
/// auth prompt, no eligible account was found, and so on). A cancelled
/// response has no <see cref="Credential"/> and causes the command layer to
/// emit <c>quit=1</c> to Git, which terminates the credential acquisition
/// pipeline without falling back to an interactive prompt.
/// </para>
/// <para>
/// Use the <see cref="Ok(ICredential)"/> and <see cref="Cancel"/> factory
/// methods rather than the constructor where possible; they make the intent
/// of each call site obvious.
/// </para>
/// <para>
/// Strongly-typed properties for newer Git credential protocol fields
/// (e.g. <c>state[]</c>, <c>continue</c>, <c>authtype</c>, <c>credential</c>,
/// <c>ephemeral</c>, <c>password_expiry_utc</c>, <c>oauth_refresh_token</c>)
/// will be added here as the corresponding capabilities are wired up; see
/// <see cref="GitCapabilities"/>. Until then, <see cref="AdditionalProperties"/>
/// remains the escape hatch for arbitrary extra output keys (for example, the
/// generic provider's <c>ntlm=allow</c> hint).
/// </para>
/// </remarks>
public class GitResponse
{
    private GitResponse(ICredential credential, bool isCancelled)
    {
        if (isCancelled && credential is not null)
        {
            throw new ArgumentException(
                "A cancelled response cannot carry a credential.",
                nameof(credential));
        }

        if (!isCancelled && credential is null)
        {
            throw new ArgumentNullException(
                nameof(credential),
                "A non-cancelled response must carry a credential. Use Cancel() instead.");
        }

        Credential = credential;
        IsCancelled = isCancelled;
    }

    /// <summary>
    /// Construct a successful response carrying the given credential.
    /// </summary>
    /// <remarks>Equivalent to <see cref="Ok(ICredential)"/>.</remarks>
    public GitResponse(ICredential credential)
        : this(credential, isCancelled: false)
    {
    }

    /// <summary>
    /// Construct a successful response carrying the given credential.
    /// </summary>
    public static GitResponse Ok(ICredential credential) =>
        new GitResponse(credential, isCancelled: false);

    /// <summary>
    /// Construct a cancellation response: the provider declined to produce a
    /// credential for this request.
    /// </summary>
    /// <remarks>
    /// The command layer translates a cancelled response into a <c>quit=1</c>
    /// line on standard output, which tells Git to abort the credential
    /// acquisition pipeline immediately without falling back to an interactive
    /// prompt. Any <see cref="AdditionalProperties"/> set on a cancelled
    /// response are ignored.
    /// </remarks>
    public static GitResponse Cancel() =>
        new GitResponse(credential: null, isCancelled: true);

    /// <summary>
    /// The credential resolved or generated for the request, or <see langword="null"/>
    /// when <see cref="IsCancelled"/> is <see langword="true"/>.
    /// </summary>
    public ICredential Credential { get; }

    /// <summary>
    /// <see langword="true"/> when the provider declined to produce a credential
    /// for this request. The command layer translates this into a <c>quit=1</c>
    /// signal to Git rather than any specific exit code.
    /// </summary>
    public bool IsCancelled { get; }

    /// <summary>
    /// Additional, untyped output to be emitted alongside the credential.
    /// </summary>
    /// <remarks>
    /// This is the legacy escape hatch for non-credential output. New code
    /// should prefer typed properties on <see cref="GitResponse"/> as they
    /// are added. Ignored on cancelled responses.
    /// </remarks>
    public IDictionary<string, string> AdditionalProperties { get; set; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
