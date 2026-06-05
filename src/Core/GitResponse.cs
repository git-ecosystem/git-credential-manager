using System;
using System.Collections.Generic;

namespace GitCredentialManager;

/// <summary>
/// Represents the response from a host provider to a Git credential helper
/// invocation (typically a <c>get</c>).
/// </summary>
/// <remarks>
/// <para>
/// A response is exactly one of three shapes:
/// </para>
/// <list type="bullet">
///   <item><see cref="Ok(ICredential)"/> -- the provider produced a credential.</item>
///   <item><see cref="Cancel"/> -- the provider declined and wants the whole
///     credential acquisition pipeline to stop (emits <c>quit=1</c>; Git aborts
///     the operation without a fallback interactive prompt).</item>
///   <item><see cref="Yield"/> -- the provider has nothing to contribute but
///     wants Git to continue trying other helpers or fall back to its built-in
///     interactive prompt (emits an empty response).</item>
/// </list>
/// <para>
/// Use the factory methods rather than the constructor where possible; they
/// make the intent of each call site obvious.
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
    private GitResponse(ICredential credential, bool isCancelled, bool isYielded)
    {
        if (isCancelled && isYielded)
        {
            throw new ArgumentException(
                "A response cannot be both cancelled and yielded.");
        }

        if ((isCancelled || isYielded) && credential is not null)
        {
            throw new ArgumentException(
                "A cancelled or yielded response cannot carry a credential.",
                nameof(credential));
        }

        if (!isCancelled && !isYielded && credential is null)
        {
            throw new ArgumentNullException(
                nameof(credential),
                "A successful response must carry a credential. Use Cancel() or Yield() instead.");
        }

        Credential = credential;
        IsCancelled = isCancelled;
        IsYielded = isYielded;
    }

    /// <summary>
    /// Construct a successful response carrying the given credential.
    /// </summary>
    /// <remarks>Equivalent to <see cref="Ok(ICredential)"/>.</remarks>
    public GitResponse(ICredential credential)
        : this(credential, isCancelled: false, isYielded: false)
    {
    }

    /// <summary>
    /// Construct a successful response carrying the given credential.
    /// </summary>
    public static GitResponse Ok(ICredential credential) =>
        new GitResponse(credential, isCancelled: false, isYielded: false);

    /// <summary>
    /// Construct a cancellation response: the provider declined to produce a
    /// credential for this request, and the whole credential acquisition
    /// pipeline should stop.
    /// </summary>
    /// <remarks>
    /// The command layer translates a cancelled response into a <c>quit=1</c>
    /// line on standard output, which tells Git to abort the credential
    /// acquisition pipeline immediately without falling back to an interactive
    /// prompt. Any <see cref="AdditionalProperties"/> set on a cancelled
    /// response are ignored.
    /// </remarks>
    public static GitResponse Cancel() =>
        new GitResponse(credential: null, isCancelled: true, isYielded: false);

    /// <summary>
    /// Construct a yielded response: the provider has nothing to contribute
    /// for this request but does not want to stop other helpers from being
    /// tried (or Git's interactive prompt from being shown).
    /// </summary>
    /// <remarks>
    /// The command layer translates a yielded response into an empty response
    /// on standard output (no credential fields, no <c>quit</c> signal). Git
    /// then proceeds to the next helper in the chain or to its built-in
    /// interactive prompt. Any <see cref="AdditionalProperties"/> set on a
    /// yielded response are ignored.
    /// </remarks>
    public static GitResponse Yield() =>
        new GitResponse(credential: null, isCancelled: false, isYielded: true);

    /// <summary>
    /// The credential resolved or generated for the request, or <see langword="null"/>
    /// when <see cref="IsCancelled"/> or <see cref="IsYielded"/> is <see langword="true"/>.
    /// </summary>
    public ICredential Credential { get; }

    /// <summary>
    /// <see langword="true"/> when the provider declined to produce a credential
    /// for this request and wants the whole credential acquisition pipeline to
    /// stop. The command layer translates this into a <c>quit=1</c> signal to
    /// Git.
    /// </summary>
    public bool IsCancelled { get; }

    /// <summary>
    /// <see langword="true"/> when the provider has nothing to contribute but
    /// does not want to stop the credential acquisition pipeline. The command
    /// layer translates this into an empty response on standard output so Git
    /// proceeds to the next helper or its interactive prompt.
    /// </summary>
    public bool IsYielded { get; }

    /// <summary>
    /// Additional, untyped output to be emitted alongside the credential.
    /// </summary>
    /// <remarks>
    /// This is the legacy escape hatch for non-credential output. New code
    /// should prefer typed properties on <see cref="GitResponse"/> as they
    /// are added. Ignored on cancelled and yielded responses.
    /// </remarks>
    public IDictionary<string, string> AdditionalProperties { get; set; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
