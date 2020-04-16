// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager.Authentication.OAuth
{
    public interface IOAuth2WebBrowser
    {
        Task<Uri> GetAuthenticationCodeAsync(Uri authorizationUri, Uri redirectUri, CancellationToken ct);
    }
}
