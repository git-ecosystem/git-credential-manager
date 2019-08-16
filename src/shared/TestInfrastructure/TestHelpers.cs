// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Microsoft.IdentityModel.JsonWebTokens;

namespace Microsoft.Git.CredentialManager.Tests
{
    public static class TestHelpers
    {
        public static JsonWebToken CreateJwt(string upn = "test")
        {
            string header = @"{ 'alg': 'none' }";
            string payload = $@"{{ 'upn': '{upn}' }}";

            return new JsonWebToken(header, payload);
        }
    }
}
