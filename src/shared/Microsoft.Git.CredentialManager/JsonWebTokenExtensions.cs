// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Microsoft.Git.CredentialManager
{
    public static class JsonWebTokenExtensions
    {
        public static string GetAzureUserName(this JsonWebToken jwt)
        {
            string idp = jwt.TryGetClaim("idp", out Claim idpClaim)
                ? idpClaim.Value.ToLowerInvariant()
                : null;

            // If the identity provider is AAD (*not* MSA) we should use the UPN claim
            if (!StringComparer.OrdinalIgnoreCase.Equals(idp, "live.com") &&
                jwt.TryGetClaim("upn", out Claim upnClaim))
            {
                return upnClaim.Value;
            }

            // For MSA IDPs or if the UPN claim is missing, we should use the 'email' claim
            if (jwt.TryGetClaim("email", out Claim emailClaim))
            {
                return emailClaim.Value;
            }

            return null;
        }
    }
}
