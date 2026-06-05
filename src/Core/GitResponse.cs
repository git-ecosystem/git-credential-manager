using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace GitCredentialManager;

/// <summary>
/// Represents the response from a host provider to a Git credential helper
/// invocation (typically a <c>get</c>).
/// </summary>
/// <remarks>
/// <para>
/// A response is exactly one of four shapes:
/// </para>
/// <list type="bullet">
///   <item><see cref="Ok(ICredential)"/> -- the provider produced a credential.</item>
///   <item><see cref="Continue(ICredential)"/> -- the provider produced a
///     credential AND signals that another round of authentication is
///     expected (emits <c>continue=1</c>; gated by the <c>state</c>
///     capability).</item>
///   <item><see cref="Cancel"/> -- the provider declined and wants the whole
///     credential acquisition pipeline to stop (emits <c>quit=1</c>; Git aborts
///     the operation without a fallback interactive prompt).</item>
///   <item><see cref="Yield"/> -- the provider has nothing to contribute but
///     wants Git to continue trying other helpers or fall back to its built-in
///     interactive prompt (emits an empty response).</item>
/// </list>
/// <para>
/// Attach opaque per-helper state to the response via <see cref="SetState"/>
/// or the fluent <see cref="WithState"/>. Both validate the key and value
/// against the wire-protocol rules and throw <see cref="ArgumentException"/>
/// on invalid input regardless of response shape. On <see cref="Cancel"/> and
/// <see cref="Yield"/> shapes the entry is then silently discarded: state has
/// no meaning when no credential is being returned.
/// </para>
/// <para>
/// <see cref="AdditionalProperties"/> is an escape hatch for arbitrary extra
/// output keys that are not captured by the <see cref="State"/> protocol capability.
/// </para>
/// </remarks>
public class GitResponse
{
    private readonly Dictionary<string, string> _state = new(StringComparer.Ordinal);
    private ReadOnlyDictionary<string, string> _stateView;

    private GitResponse(ICredential credential, bool isContinue, bool isCancelled, bool isYielded)
    {
        // At most one of Continue, Cancel, Yield may be set (Ok is "none of them").
        if ((isContinue && isCancelled) ||
            (isContinue && isYielded) ||
            (isCancelled && isYielded))
        {
            throw new ArgumentException(
                "A response can be at most one of Continue, Cancel, or Yield.");
        }

        bool hasCredential = credential is not null;

        if ((isCancelled || isYielded) && hasCredential)
        {
            throw new ArgumentException(
                "A cancelled or yielded response cannot carry a credential.",
                nameof(credential));
        }

        if (!isCancelled && !isYielded && !hasCredential)
        {
            throw new ArgumentNullException(
                nameof(credential),
                "A non-cancelled, non-yielded response must carry a credential. Use Cancel() or Yield() instead.");
        }

        Credential = credential;
        IsContinue = isContinue;
        IsCancelled = isCancelled;
        IsYielded = isYielded;
    }

    /// <summary>
    /// Construct a successful response carrying the given credential.
    /// </summary>
    /// <remarks>Equivalent to <see cref="Ok(ICredential)"/>.</remarks>
    public GitResponse(ICredential credential)
        : this(credential, isContinue: false, isCancelled: false, isYielded: false)
    {
    }

    /// <summary>
    /// Construct a successful response carrying the given credential.
    /// </summary>
    public static GitResponse Ok(ICredential credential) =>
        new GitResponse(credential, isContinue: false, isCancelled: false, isYielded: false);

    /// <summary>
    /// Construct a successful response carrying the given credential and
    /// signalling that another round of authentication is expected.
    /// </summary>
    /// <remarks>
    /// The command layer translates this into a <c>continue=1</c> line on
    /// standard output, gated by the <c>state</c> capability. Common in
    /// multistage HTTP authentication (NTLM/Kerberos) and any flow where the
    /// helper wants to be invoked again after the next server response.
    /// </remarks>
    public static GitResponse Continue(ICredential credential) =>
        new GitResponse(credential, isContinue: true, isCancelled: false, isYielded: false);

    /// <summary>
    /// Construct a cancellation response: the provider declined to produce a
    /// credential for this request, and the whole credential acquisition
    /// pipeline should stop.
    /// </summary>
    /// <remarks>
    /// The command layer translates a cancelled response into a <c>quit=1</c>
    /// line on standard output, which tells Git to abort the credential
    /// acquisition pipeline immediately without falling back to an interactive
    /// prompt. Any <see cref="AdditionalProperties"/> or state set on a
    /// cancelled response are ignored.
    /// </remarks>
    public static GitResponse Cancel() =>
        new GitResponse(credential: null, isContinue: false, isCancelled: true, isYielded: false);

    /// <summary>
    /// Construct a yielded response: the provider has nothing to contribute
    /// for this request but does not want to stop other helpers from being
    /// tried (or Git's interactive prompt from being shown).
    /// </summary>
    /// <remarks>
    /// The command layer translates a yielded response into an empty response
    /// on standard output (no credential fields, no <c>quit</c> signal). Git
    /// then proceeds to the next helper in the chain or to its built-in
    /// interactive prompt. Any <see cref="AdditionalProperties"/> or state
    /// set on a yielded response are ignored.
    /// </remarks>
    public static GitResponse Yield() =>
        new GitResponse(credential: null, isContinue: false, isCancelled: false, isYielded: true);

    /// <summary>
    /// The credential resolved or generated for the request, or <see langword="null"/>
    /// when <see cref="IsCancelled"/> or <see cref="IsYielded"/> is <see langword="true"/>.
    /// </summary>
    public ICredential Credential { get; }

    /// <summary>
    /// <see langword="true"/> when the provider expects a further round of
    /// authentication. The command layer translates this into a <c>continue=1</c>
    /// signal to Git (gated by the <c>state</c> capability).
    /// </summary>
    public bool IsContinue { get; }

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
    /// Read-only view of the per-helper state to emit alongside the credential,
    /// gated by the <c>state</c> capability.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Keys are stored WITHOUT the <see cref="Constants.CredentialProtocol.GcmStatePrefix"/>;
    /// the command layer prepends it on the way out.
    /// </para>
    /// <para>
    /// Mutate via <see cref="SetState"/> or the fluent <see cref="WithState"/>;
    /// reads go through the standard <see cref="IReadOnlyDictionary{TKey, TValue}"/>
    /// surface (indexer, <c>TryGetValue</c>, <c>ContainsKey</c>, foreach). On
    /// <see cref="Cancel"/> and <see cref="Yield"/> responses, mutation
    /// silently no-ops and this view stays empty.
    /// </para>
    /// </remarks>
    public IReadOnlyDictionary<string, string> State => _stateView ??= _state.AsReadOnly();

    /// <summary>
    /// Set a single state entry.
    /// </summary>
    /// <remarks>
    /// <exception cref="ArgumentException">
    /// Throws if the key or value contains invalid characters or an invalid key prefix.
    /// </exception>
    /// <para>
    /// On <see cref="Cancel"/> and <see cref="Yield"/> responses, the entry is
    /// silently discarded: state has no meaning when no credential is being returned.
    /// </para>
    /// </remarks>
    public void SetState(string key, string value)
    {
        GitStateValidation.ValidateKey(key);
        GitStateValidation.ValidateValue(value);

        if (IsCancelled || IsYielded)
        {
            return;
        }

        _state[key] = value;
    }

    /// <summary>
    /// Fluent equivalent of <see cref="SetState"/>: sets one state entry and
    /// returns the same response for chaining.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Throws if the key or value contains invalid characters or an invalid key prefix.
    /// </exception>
    /// <remarks>
    /// Same silent-on-Cancel/Yield semantics as <see cref="SetState"/>.
    /// </remarks>
    public GitResponse WithState(string key, string value)
    {
        SetState(key, value);
        return this;
    }

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
