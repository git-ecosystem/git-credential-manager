using System;
using Microsoft.Identity.Client;

namespace GitCredentialManager.Authentication.Entra;

public interface IEntraAccount : IEquatable<IEntraAccount>
{
    /// <summary>
    /// Opaque, stable identifier for the account in MSAL's cache. Use this to refer to
    /// the account from persistent records — it survives UPN renames.
    /// </summary>
    string HomeAccountId { get; }

    /// <summary>
    /// User principal name (typically an email address); suitable for display.
    /// </summary>
    string UserName { get; }
}

public sealed class EntraAccount : IEntraAccount
{
    internal static EntraAccount FromMsalAccount(IAccount msalAccount)
    {
        EnsureArgument.NotNull(msalAccount, nameof(msalAccount));
        return new EntraAccount(msalAccount.HomeAccountId.Identifier, msalAccount.Username)
        {
            MsalAccount = msalAccount
        };
    }

    public EntraAccount(string homeAccountId, string userName)
    {
        UserName = userName;
        HomeAccountId = homeAccountId;
    }

    public string HomeAccountId { get; }
    public string UserName { get; }
    internal IAccount MsalAccount { get; init; }

    // Both fields are compared case-insensitively to match MSAL: AccountId.Equals on the
    // identifier uses OrdinalIgnoreCase, and UPNs are case-insensitive per RFC 5321.
    public bool Equals(IEntraAccount other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return StringComparer.OrdinalIgnoreCase.Equals(HomeAccountId, other.HomeAccountId)
               && StringComparer.OrdinalIgnoreCase.Equals(UserName, other.UserName);
    }

    public override bool Equals(object obj) => obj is IEntraAccount other && Equals(other);

    public override int GetHashCode()
    {
        int h1 = HomeAccountId is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(HomeAccountId);
        int h2 = UserName      is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(UserName);
        unchecked { return (h1 * 397) ^ h2; }
    }
}
