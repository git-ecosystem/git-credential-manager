using System;

namespace GitCredentialManager.Authentication.OAuth
{
    public class OAuth2AuthorizationCodeResult
    {
        public OAuth2AuthorizationCodeResult(string code, Uri redirectUri = null, string codeVerifier = null)
        {
            Code = code;
            RedirectUri = redirectUri;
            CodeVerifier = codeVerifier;
        }

        public string Code { get; }
        public Uri RedirectUri { get; }
        public string CodeVerifier { get; }
    }
}
