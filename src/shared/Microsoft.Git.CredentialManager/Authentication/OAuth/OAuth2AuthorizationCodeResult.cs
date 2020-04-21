// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Git.CredentialManager.Authentication.OAuth
{
    public class OAuth2AuthorizationCodeResult
    {
        public OAuth2AuthorizationCodeResult(string code, string codeVerifier = null)
        {
            Code = code;
            CodeVerifier = codeVerifier;
        }

        public string Code { get; }

        public string CodeVerifier { get; }
    }
}
