using System;
using System.Collections.Generic;

namespace GitCredentialManager;

/// <summary>
/// Capabilities of the Git credential helper protocol introduced in Git 2.46.
/// </summary>
/// <remarks>
/// <para>
/// A capability is negotiated between Git and the credential helper: each side
/// emits <c>capability[]</c> entries describing what it understands, and only
/// the intersection may be safely exercised in the rest of the exchange.
/// </para>
/// <para>
/// Unrecognized capability names are silently discarded per
/// <see href="https://git-scm.com/docs/git-credential">git-credential(1)</see>.
/// </para>
/// <para>
/// <see cref="Constants.SupportedCapabilities"/>
/// determines which capabilities GCM itself advertises back to Git.
/// </para>
/// </remarks>
[Flags]
public enum GitCapabilities
{
    /// <summary>
    /// No capabilities are supported.
    /// </summary>
    None = 0,

    /// <summary>
    /// The <c>state[]</c> and <c>continue</c> attributes are understood.
    /// </summary>
    /// <remarks>
    /// Provides for opaque per-helper state that Git stores between
    /// invocations and replays on the next call, plus a continuation
    /// flag that signals a non-final authentication step is expected.
    /// </remarks>
    State = 1 << 0,
}

/// <summary>
/// Helpers for parsing and advertising Git credential protocol capabilities.
/// </summary>
public static class GitCapabilitiesUtils
{
    /// <summary>
    /// Parse a single capability name (e.g. <c>"authtype"</c>) into a <see cref="GitCapabilities"/>
    /// flag. Unrecognized names parse to <see cref="GitCapabilities.None"/>.
    /// </summary>
    /// <remarks>
    /// Names from Git are lowercase per the protocol; matching is done
    /// case-insensitively for safety. New capability names should be added
    /// here, mirroring the values added to <see cref="GitCapabilities"/>.
    /// </remarks>
    public static GitCapabilities ParseName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return GitCapabilities.None;
        }

        // Names from Git are lowercase; parse case-insensitively for safety.
        // New entries will be added here as each capability's input/output
        // handling is implemented.
        return name.ToLowerInvariant() switch
        {
            "state" => GitCapabilities.State,
            _ => GitCapabilities.None,
        };
    }

    /// <summary>
    /// Render a single <see cref="GitCapabilities"/> flag to its on-the-wire
    /// protocol name (e.g. <c>"authtype"</c>).
    /// </summary>
    /// <remarks>
    /// The protocol name is always lowercase. New entries must be added here
    /// in lockstep with new <see cref="GitCapabilities"/> flag values to avoid
    /// emitting an incorrect name to Git.
    /// </remarks>
    public static string ToProtocolName(GitCapabilities capability)
    {
        // Add each flag's protocol name here as the capability is wired up.
        // The default lowercase enum name is intentionally NOT used because
        // some protocol names will not be a single token (e.g. authtype is fine
        // but a hypothetical "PasswordExpiryUtc" would have to map to a
        // protocol name distinct from its .NET enum name).
        return capability switch
        {
            GitCapabilities.State => "state",
            GitCapabilities.None => throw new ArgumentException(
                "Cannot render the None capability to a protocol name.",
                nameof(capability)),
            _ => throw new ArgumentOutOfRangeException(
                nameof(capability),
                capability,
                "No protocol name mapping is defined for the given capability."),
        };
    }

    /// <summary>
    /// Enumerate each individual <see cref="GitCapabilities"/> flag set in
    /// <paramref name="capabilities"/>, rendered to its on-the-wire protocol name.
    /// </summary>
    /// <remarks>
    /// Returns an empty sequence for <see cref="GitCapabilities.None"/>. Each
    /// emitted name comes from <see cref="ToProtocolName"/>.
    /// </remarks>
    public static IEnumerable<string> ToProtocolNames(GitCapabilities capabilities)
    {
        if (capabilities == GitCapabilities.None)
        {
            yield break;
        }

        foreach (GitCapabilities flag in Enum.GetValues<GitCapabilities>())
        {
            if (flag == GitCapabilities.None)
            {
                continue;
            }

            if ((capabilities & flag) == flag)
            {
                yield return ToProtocolName(flag);
            }
        }
    }
}
