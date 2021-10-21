using System;

namespace GitCredentialManager.Authentication.OAuth
{
    public class OAuth2Exception : Exception
    {
        public OAuth2Exception(string message) : base(message) { }

        public OAuth2Exception(string message, Exception innerException) : base(message, innerException) { }
    }
}
