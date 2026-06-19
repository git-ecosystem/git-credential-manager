using System;

namespace GitCredentialManager.Authentication.OAuth;

/// <summary>
/// The mechanism the authorization server uses to return authorization response
/// parameters to the redirect URI.
/// </summary>
public enum OAuth2ResponseMode
{
    /// <summary>
    /// Use the default response mode as determined by the authorization server.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Parameters are encoded in the query component of the redirect URI.
    /// </summary>
    Query,

    /// <summary>
    /// Parameters are encoded in the fragment component of the redirect URI.
    /// </summary>
    Fragment,

    /// <summary>
    /// Parameters are returned as an HTML form that is auto-submitted as an
    /// <c>application/x-www-form-urlencoded</c> POST to the redirect URI, as
    /// described by the OAuth 2.0 Form Post Response Mode specification.
    /// </summary>
    FormPost,
}

public static class OAuth2ResponseModeExtensions
{
    /// <summary>
    /// Get the wire value for the <c>response_mode</c> authorization request parameter.
    /// </summary>
    public static string GetParameterValue(this OAuth2ResponseMode mode)
    {
        switch (mode)
        {
            case OAuth2ResponseMode.Default:
                return null;
            case OAuth2ResponseMode.Query:
                return OAuth2Constants.AuthorizationEndpoint.QueryResponseMode;
            case OAuth2ResponseMode.Fragment:
                return OAuth2Constants.AuthorizationEndpoint.FragmentResponseMode;
            case OAuth2ResponseMode.FormPost:
                return OAuth2Constants.AuthorizationEndpoint.FormPostResponseMode;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown OAuth2 response mode.");
        }
    }

    /// <summary>
    /// Try to parse a <c>response_mode</c> wire value into an <see cref="OAuth2ResponseMode"/>.
    /// </summary>
    public static bool TryParse(string value, out OAuth2ResponseMode mode)
    {
        mode = OAuth2ResponseMode.Default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (StringComparer.OrdinalIgnoreCase.Equals(value, OAuth2Constants.AuthorizationEndpoint.QueryResponseMode))
        {
            mode = OAuth2ResponseMode.Query;
            return true;
        }

        if (StringComparer.OrdinalIgnoreCase.Equals(value, OAuth2Constants.AuthorizationEndpoint.FragmentResponseMode))
        {
            mode = OAuth2ResponseMode.Fragment;
            return true;
        }

        // Accept both "form_post" (wire value) and "formpost" for convenience.
        if (StringComparer.OrdinalIgnoreCase.Equals(value, OAuth2Constants.AuthorizationEndpoint.FormPostResponseMode) ||
            StringComparer.OrdinalIgnoreCase.Equals(value, "formpost"))
        {
            mode = OAuth2ResponseMode.FormPost;
            return true;
        }

        return false;
    }
}
