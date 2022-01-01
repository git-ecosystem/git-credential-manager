using System;

namespace GitCredentialManager.Authentication.OAuth
{
    /// <summary>
    /// Represents the various OAuth2 endpoints for an <see cref="OAuth2Client"/>.
    /// </summary>
    public record OAuth2ServerEndpoints
    {
        public OAuth2ServerEndpoints(Uri authorizationEndpoint, Uri tokenEndpoint, Uri deviceAuthorizationEndPoint = null)
        {
            EnsureArgument.AbsoluteUri(authorizationEndpoint, nameof(authorizationEndpoint));
            EnsureArgument.AbsoluteUri(tokenEndpoint, nameof(tokenEndpoint));
            if (deviceAuthorizationEndPoint != null)
            {
                EnsureArgument.AbsoluteUri(deviceAuthorizationEndPoint, nameof(deviceAuthorizationEndPoint));
            }

            AuthorizationEndpoint = authorizationEndpoint;
            TokenEndpoint = tokenEndpoint;
            DeviceAuthorizationEndpoint = deviceAuthorizationEndPoint;
        }

        public Uri AuthorizationEndpoint { get; }

        public Uri TokenEndpoint { get; }

        public Uri DeviceAuthorizationEndpoint { get; }
    }
}
