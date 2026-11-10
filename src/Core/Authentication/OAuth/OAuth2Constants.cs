
namespace GitCredentialManager.Authentication.OAuth
{
    public static class OAuth2Constants
    {
        public const string ClientIdParameter = "client_id";
        public const string ClientSecretParameter = "client_secret";
        public const string RedirectUriParameter = "redirect_uri";
        public const string ScopeParameter = "scope";
        public const string Trace2Category = "oauth2";

        public static class TokenTypes
        {
            public const string AccessToken = "urn:ietf:params:oauth:token-type:access_token";
            public const string RefreshToken = "urn:ietf:params:oauth:token-type:refresh_token";
            public const string IdToken = "urn:ietf:params:oauth:token-type:id_token";
            public const string Saml1Assertion = "urn:ietf:params:oauth:token-type:saml1";
            public const string Saml2Assertion = "urn:ietf:params:oauth:token-type:saml2";
        }

        public static class AuthorizationEndpoint
        {
            public const string StateParameter = "state";
            public const string AuthorizationCodeResponseType = "code";
            public const string ResponseTypeParameter = "response_type";
            public const string ResponseModeParameter = "response_mode";
            public const string QueryResponseMode = "query";
            public const string FragmentResponseMode = "fragment";
            public const string FormPostResponseMode = "form_post";
            public const string PkceChallengeParameter = "code_challenge";
            public const string PkceChallengeMethodParameter = "code_challenge_method";
            public const string PkceChallengeMethodPlain = "plain";
            public const string PkceChallengeMethodS256 = "S256";
        }

        public static class AuthorizationGrantResponse
        {
            public const string AuthorizationCodeParameter = "code";
            public const string ErrorCodeParameter = "error";
            public const string ErrorDescriptionParameter = "error_description";
            public const string ErrorUriParameter = "error_uri";
            public const string StateParameter = "state";
        }

        public static class TokenEndpoint
        {
            public const string GrantTypeParameter = "grant_type";
            public const string AuthorizationCodeGrantType = "authorization_code";
            public const string RefreshTokenGrantType = "refresh_token";
            public const string TokenExchangeGrantType = "urn:ietf:params:oauth:grant-type:token-exchange";
            public const string JwtBearerGrantType = "urn:ietf:params:oauth:grant-type:jwt-bearer";
            public const string SubjectTokenParameter = "subject_token";
            public const string SubjectTokenTypeParameter = "subject_token_type";
            public const string PkceVerifierParameter = "code_verifier";
            public const string AuthorizationCodeParameter = "code";
            public const string RefreshTokenParameter = "refresh_token";
            public const string AssertionParameter = "assertion";
        }

        public static class DeviceAuthorization
        {
            public const string GrantTypeParameter = "grant_type";
            public const string DeviceCodeParameter = "device_code";
            public const string DeviceCodeGrantType = "urn:ietf:params:oauth:grant-type:device_code";

            public static class Errors
            {
                public const string AuthorizationPending = "authorization_pending";
                public const string SlowDown = "slow_down";
            }
        }
    }
}
