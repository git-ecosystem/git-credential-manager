using System;

namespace GitCredentialManager.Authentication.OAuth
{
    public class OAuth2Exception : Exception
    {
        public string ErrorCode { get; init; }

        public OAuth2Exception(string message) : base(message) { }

        public OAuth2Exception(string message, Exception innerException) : base(message, innerException) { }
    }
}
