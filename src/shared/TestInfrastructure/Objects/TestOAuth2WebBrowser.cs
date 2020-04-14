// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager.Authentication.OAuth;

namespace Microsoft.Git.CredentialManager.Tests.Objects
{
    public class TestOAuth2WebBrowser : IOAuth2WebBrowser
    {
        private readonly HttpClient _httpClient;

        public TestOAuth2WebBrowser(HttpMessageHandler httpHandler)
        {
            _httpClient = new HttpClient(httpHandler);
        }

        public async Task<Uri> GetAuthenticationCodeAsync(Uri authorizationUri, Uri redirectUri, CancellationToken ct)
        {
            using (var response = await _httpClient.SendAsync(HttpMethod.Get, authorizationUri))
            {
                response.EnsureSuccessStatusCode();
                return response.Headers.Location;
            }
        }
    }
}
