using System;
using Microsoft.Identity.Client.AppConfig;

namespace GitCredentialManager.Authentication.Entra;

public record ManagedIdentity
{
    private readonly ManagedIdentityId _id;

    public static readonly ManagedIdentity System = new("system", ManagedIdentityId.SystemAssigned);

    public static ManagedIdentity FromClientId(Guid clientId)
    {
        var id = clientId.ToString("D");
        return new($"id://{id}", ManagedIdentityId.WithUserAssignedClientId(id));
    }

    public static ManagedIdentity FromResourceId(Guid resourceId)
    {
        var id = resourceId.ToString("D");
        return new($"resource://{id}", ManagedIdentityId.WithUserAssignedResourceId(id));
    }

    public static ManagedIdentity Create(string id) =>
        TryCreate(id, out ManagedIdentity mi)
            ? mi
            : throw new ArgumentException($"Invalid managed identity id '{id}'", nameof(id));

    public static bool TryCreate(string id, out ManagedIdentity mi)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            mi = null;
            return false;
        }

        if (StringComparer.OrdinalIgnoreCase.Equals(id, "system"))
        {
            mi = System;
            return true;
        }

        // {uuid} => user-assigned client ID
        if (Guid.TryParse(id, out Guid guid))
        {
            mi = FromClientId(guid);
            return true;
        }

        // (resource|id)://{uuid}
        if (Uri.TryCreate(id, UriKind.Absolute, out Uri uri) && Guid.TryParse(uri.Host, out guid))
        {
            if (StringComparer.OrdinalIgnoreCase.Equals(uri.Scheme, "id"))
            {
                mi = FromClientId(guid);
                return true;
            }

            if (StringComparer.OrdinalIgnoreCase.Equals(uri.Scheme, "resource"))
            {
                mi = FromResourceId(guid);
                return true;
            }
        }

        // Unknown
        mi = null;
        return false;
    }

    private ManagedIdentity(string idStr, ManagedIdentityId id)
    {
        Id = idStr;
        _id = id;
    }

    public string Id { get; }

    public static implicit operator ManagedIdentityId(ManagedIdentity mi) => mi._id;
}
