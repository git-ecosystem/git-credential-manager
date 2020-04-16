// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;

namespace Microsoft.Git.CredentialManager.Authentication.OAuth
{
    public interface IOAuth2NonceGenerator
    {
        string CreateNonce();
    }

    public class OAuth2NonceGenerator : IOAuth2NonceGenerator
    {
        public string CreateNonce()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
