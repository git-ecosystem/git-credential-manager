using System;
using System.Collections.Generic;

namespace GitCredentialManager.Authentication.OAuth;

/// <summary>
/// Options for requesting a token exchange from an OAuth 2.0 authorization server.
/// </summary>
public sealed record OAuth2TokenExchangeOptions
{
    /// <summary>
    /// Optional set of scopes to request for the new token.
    /// </summary>
    public IList<string> Scopes { get; init; } = [];

    /// <summary>
    /// Optional set of logical names of the target service where the client
    /// intends to use the requested security token.
    /// </summary>
    public IList<string> Audiences { get; init; } = [];

    /// <summary>
    /// Optional set of URIs that indicates the target service or resource where
    /// the client intends to use the requested security token.
    /// </summary>
    public IList<Uri> Resources { get; init; } = [];

    /// <summary>
    /// A required security token that represents the identity of the party on
    /// behalf of whom the request is being made.
    /// </summary>
    public string SubjectToken { get; init; }

    /// <summary>
    /// The type of the security token in the <see cref="SubjectToken"/> parameter.
    /// </summary>
    public OAuth2TokenType SubjectTokenType { get; init; }

    /// <summary>
    /// The type of the requested security token.
    /// </summary>
    /// <remarks>
    /// If the requested type is unspecified, the issued token type is at the discretion
    /// of the authorization server and may be dictated by knowledge of the requirements
    /// of the service or resource indicated by the resource or audience parameter.
    /// </remarks>
    public OAuth2TokenType? RequestedTokenType { get; init; }

    /// <summary>
    /// The type of the security token in the <see cref="ActorToken"/> parameter.
    /// </summary>
    public OAuth2TokenType? ActorTokenType { get; init; }

    /// <summary>
    /// Optional security token that represents the identity of the acting party.
    /// </summary>
    public string ActorToken { get; init; }

    public bool IsValid(out string error)
    {
        if (string.IsNullOrEmpty(SubjectToken))
        {
            error = "Subject token must be provided.";
            return false;
        }

        if (SubjectTokenType == default)
        {
            error = "Subject token type must be specified.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(SubjectTokenType.Identifier))
        {
            error = "Subject token type is invalid.";
            return false;
        }

        // Must have actor token type if actor token is provided, and vice versa.
        if (ActorTokenType.HasValue == string.IsNullOrWhiteSpace(ActorToken))
        {
            error = "Actor token and actor token type must be specified together.";
            return false;
        }

        if (ActorTokenType.HasValue && string.IsNullOrWhiteSpace(ActorTokenType.Value.Identifier))
        {
            error = "Actor token type is invalid.";
            return false;
        }

        if (RequestedTokenType.HasValue && string.IsNullOrWhiteSpace(RequestedTokenType.Value.Identifier))
        {
            error = "Requested token type is invalid.";
            return false;
        }

        foreach (string audience in Audiences ?? [])
        {
            if (string.IsNullOrWhiteSpace(audience))
            {
                error = "All audiences must be non-null and not empty.";
                return false;
            }
        }

        foreach (Uri resource in Resources ?? [])
        {
            if (resource is null)
            {
                error = "All resources must be non-null.";
                return false;
            }

            if (!resource.IsAbsoluteUri)
            {
                error = "All resources must be absolute URIs.";
                return false;
            }

            if (resource.Fragment.Length > 0)
            {
                error = "All resources must not contain fragments.";
                return false;
            }
        }

        error = null;
        return true;
    }

    public ICollection<KeyValuePair<string, string>> ToRequestParameters()
    {
        if (!IsValid(out string error))
        {
            throw new InvalidOperationException(error);
        }

        var data = new List<KeyValuePair<string, string>>
        {
            new(OAuth2Constants.TokenEndpoint.GrantTypeParameter, OAuth2Constants.TokenEndpoint.TokenExchangeGrantType),
            new(OAuth2Constants.TokenEndpoint.SubjectTokenParameter, SubjectToken),
            new(OAuth2Constants.TokenEndpoint.SubjectTokenTypeParameter, SubjectTokenType.Identifier)
        };

        if (Scopes?.Count > 0)
        {
            string scopesStr = string.Join(" ", Scopes);
            data.Add(new (OAuth2Constants.ScopeParameter, scopesStr));
        }

        foreach (string audience in Audiences ?? [])
        {
            data.Add(new (OAuth2Constants.TokenEndpoint.AudienceParameter, audience));
        }

        foreach (Uri resource in Resources ?? [])
        {
            data.Add(new (OAuth2Constants.TokenEndpoint.ResourceParameter, resource.AbsoluteUri));
        }

        if (ActorTokenType is { } actorTokenType && !string.IsNullOrWhiteSpace(ActorToken))
        {
            data.Add(new (OAuth2Constants.TokenEndpoint.ActorTokenParameter, ActorToken));
            data.Add(new (OAuth2Constants.TokenEndpoint.ActorTokenTypeParameter, actorTokenType.Identifier));
        }

        if (RequestedTokenType is { } requestedTokenType)
        {
            data.Add(new (OAuth2Constants.TokenEndpoint.RequestedTokenTypeParameter, requestedTokenType.Identifier));
        }

        return data;
    }
}
