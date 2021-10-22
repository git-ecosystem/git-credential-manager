using System;

namespace GitCredentialManager.Authentication.OAuth
{
    /// <summary>
    /// Represents the various OAuth2 endpoints for an <see cref="OAuth2Client"/>.
    /// </summary>
    public class OAuth2ServerEndpoints
    {
        private Uri _deviceAuthorizationEndpoint;

        public OAuth2ServerEndpoints(Uri authorizationEndpoint, Uri tokenEndpoint)
        {
            EnsureArgument.AbsoluteUri(authorizationEndpoint, nameof(authorizationEndpoint));
            EnsureArgument.AbsoluteUri(tokenEndpoint, nameof(tokenEndpoint));

            AuthorizationEndpoint = authorizationEndpoint;
            TokenEndpoint = tokenEndpoint;
        }

        public Uri AuthorizationEndpoint { get; }

        public Uri TokenEndpoint { get; }

        public Uri DeviceAuthorizationEndpoint
        {
            get => _deviceAuthorizationEndpoint;
            set
            {
                if (value != null)
                {
                    EnsureArgument.AbsoluteUri(value, nameof(value));
                }

                _deviceAuthorizationEndpoint = value;
            }
        }
    }
}
