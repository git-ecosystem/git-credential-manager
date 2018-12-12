// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager.Authentication;
using Microsoft.Git.CredentialManager.Tests.Objects;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests.Authentication
{
    public class NtlmAuthenticationTests
    {
        [Fact]
        public async Task NtlmAuthentication_IsNtlmSupportedAsync_NullUri_ThrowsException()
        {
            var context = new TestCommandContext();
            var ntlmAuth = new NtlmAuthentication(context);

            await Assert.ThrowsAsync<ArgumentNullException>(() => ntlmAuth.IsNtlmSupportedAsync(null));
        }

        [PlatformFact(Platform.MacOS, Platform.Linux)]
        public async Task NtlmAuthentication_NonWindows_IsNtlmSupportedAsync_ReturnsFalse()
        {
            var context = new TestCommandContext();
            var uri = new Uri("https://example.com");

            var ntlmResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            ntlmResponse.Headers.WwwAuthenticate.ParseAdd("NTLM [test-challenge-string]");

            var httpHandler = new TestHttpMessageHandler {ThrowOnUnexpectedRequest = true};
            httpHandler.Setup(HttpMethod.Head, uri, ntlmResponse);

            var httpFactory = new TestHttpClientFactory {MessageHandler = httpHandler};
            var ntlmAuth = new NtlmAuthentication(context, httpFactory);

            bool result = await ntlmAuth.IsNtlmSupportedAsync(uri);

            Assert.False(result);
            httpHandler.AssertRequest(HttpMethod.Head, uri, expectedNumberOfCalls: 0);
        }

        [PlatformFact(Platform.Windows)]
        public async Task NtlmAuthentication_Windows_IsNtlmSupportedAsync_NtlmHeader_ReturnsTrue()
        {
            var context = new TestCommandContext();
            var uri = new Uri("https://example.com");

            var ntlmResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            ntlmResponse.Headers.WwwAuthenticate.ParseAdd("NTLM [test-challenge-string]");

            var httpHandler = new TestHttpMessageHandler {ThrowOnUnexpectedRequest = true};
            httpHandler.Setup(HttpMethod.Head, uri, ntlmResponse);

            var httpFactory = new TestHttpClientFactory {MessageHandler = httpHandler};
            var ntlmAuth = new NtlmAuthentication(context, httpFactory);

            bool result = await ntlmAuth.IsNtlmSupportedAsync(uri);

            Assert.True(result);
            httpHandler.AssertRequest(HttpMethod.Head, uri, expectedNumberOfCalls: 1);
        }

        [PlatformFact(Platform.Windows)]
        public async Task NtlmAuthentication_Windows_IsNtlmSupportedAsync_NoNtlmHeader_ReturnsFalse()
        {
            var context = new TestCommandContext();
            var uri = new Uri("https://example.com");

            var ntlmResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            ntlmResponse.Headers.WwwAuthenticate.ParseAdd("Bearer");
            ntlmResponse.Headers.WwwAuthenticate.ParseAdd("NotNTLM test test NTLM test");

            var httpHandler = new TestHttpMessageHandler {ThrowOnUnexpectedRequest = true};
            httpHandler.Setup(HttpMethod.Head, uri, ntlmResponse);

            var httpFactory = new TestHttpClientFactory {MessageHandler = httpHandler};
            var ntlmAuth = new NtlmAuthentication(context, httpFactory);

            bool result = await ntlmAuth.IsNtlmSupportedAsync(uri);

            Assert.False(result);
            httpHandler.AssertRequest(HttpMethod.Head, uri, expectedNumberOfCalls: 1);
        }
    }
}
