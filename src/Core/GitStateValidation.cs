using System;

namespace GitCredentialManager;

/// <summary>
/// Validation rules for keys and values that Git Credential Manager emits
/// as part of the <c>state[]</c> protocol attribute.
/// </summary>
/// <remarks>
/// <para>
/// The Git credential protocol's wire format is line-oriented and key/value
/// separated by <c>=</c>. State entries are emitted as <c>state[]={prefix}{key}={value}</c>,
/// so neither key nor value may contain a newline or NUL, and the key may not contain an
/// additional <c>=</c> because the first one is what splits the field.
/// We also forbid the empty key and reject keys that already start with
/// <see cref="Constants.CredentialProtocol.GcmStatePrefix"/> because the framework adds
/// that prefix on the way out.
/// </para>
/// </remarks>
public static class GitStateValidation
{
    private const char LF = '\n';
    private const char NUL = '\0';
    private const char EQ = '=';

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="key"/> is a legal state
    /// entry key (non-empty, no <c>=</c>/newline/NUL, no <c>gcm.</c> prefix).
    /// </summary>
    public static bool IsValidKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        foreach (char c in key)
        {
            if (c == EQ || c == LF || c == NUL)
            {
                return false;
            }
        }

        if (key.StartsWith(Constants.CredentialProtocol.GcmStatePrefix, StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a legal
    /// state entry value (non-null, no newline/NUL).
    /// </summary>
    public static bool IsValidValue(string value)
    {
        if (value is null)
        {
            return false;
        }

        foreach (char c in value)
        {
            if (c == LF || c == NUL)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Throws <see cref="ArgumentException"/> if <paramref name="key"/> is not
    /// a legal state entry key.
    /// </summary>
    public static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("State key cannot be null or whitespace.", nameof(key));
        }

        foreach (char c in key)
        {
            if (c == EQ || c == LF || c == NUL)
            {
                throw new ArgumentException(
                    "State key cannot contain '=', newline, or NUL characters.",
                    nameof(key));
            }
        }

        if (key.StartsWith(Constants.CredentialProtocol.GcmStatePrefix, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"State key cannot start with '{Constants.CredentialProtocol.GcmStatePrefix}'; " +
                "the prefix is reserved and added automatically when state is emitted.",
                nameof(key));
        }
    }

    /// <summary>
    /// Throws <see cref="ArgumentException"/> if <paramref name="value"/> is
    /// not a legal state entry value.
    /// </summary>
    public static void ValidateValue(string value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value), "State value cannot be null.");
        }

        foreach (char c in value)
        {
            if (c == LF || c == NUL)
            {
                throw new ArgumentException(
                    "State value cannot contain newline or NUL characters.",
                    nameof(value));
            }
        }
    }
}

