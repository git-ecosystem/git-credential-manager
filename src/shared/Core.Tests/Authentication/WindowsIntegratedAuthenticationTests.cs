using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GitCredentialManager.Authentication;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace GitCredentialManager.Tests.Authentication
{
    public class WindowsIntegratedAuthenticationTests
    {
        private const string NegotiateHeader = "Negotiate [test-challenge-string]";
        private const string NtlmHeader      = "NTLM [test-challenge-string]";

        [Fact]
        public async Task WindowsIntegratedAuthentication_GetIsSupportedAsync_NullUri_ThrowsException()
        {
            var context = new TestCommandContext();
            var wiaAuth = new WindowsIntegratedAuthentication(context);

            await Assert.ThrowsAsync<ArgumentNullException>(() => wiaAuth.GetAuthenticationTypesAsync(null));
        }

        [Fact]
        public async Task WindowsIntegratedAuthentication_GetIsSupportedAsync_NegotiateAndNtlm_ReturnsTrue()
        {
            await TestGetIsSupportedAsync(new[] {NegotiateHeader, NtlmHeader}, expected: WindowsAuthenticationTypes.All);

            // Also check with the headers in the other order
            await TestGetIsSupportedAsync(new[] {NtlmHeader, NegotiateHeader}, expected: WindowsAuthenticationTypes.All);
        }

        [Fact]
        public async Task WindowsIntegratedAuthentication_Windows_GetIsSupportedAsync_Ntlm_ReturnsTrue()
        {
            await TestGetIsSupportedAsync(new[]{NtlmHeader}, expected: WindowsAuthenticationTypes.Ntlm);
        }

        [Fact]
        public async Task WindowsIntegratedAuthentication_Windows_GetIsSupportedAsync_Negotiate_ReturnsTrue()
        {
            await TestGetIsSupportedAsync(new[]{NegotiateHeader}, expected: WindowsAuthenticationTypes.Negotiate);
        }

        [Fact]
        public async Task WindowsIntegratedAuthentication_Windows_GetIsSupportedAsync_NoHeaders_ReturnsFalse()
        {
            await TestGetIsSupportedAsync(new string[0], expected: WindowsAuthenticationTypes.None);
        }

        [Fact]
        public async Task WindowsIntegratedAuthentication_Windows_GetIsSupportedAsync_NoWiaHeaders_ReturnsFalse()
        {
            await TestGetIsSupportedAsync(
                new[]
                {
                    "Bearer",
                    "Bearer foo",
                    "NotNTLM test test NTLM test",
                    "NotNegotiate test test Negotiate test",
                    "NotKerberos test test Negotiate test"
                },
                expected: WindowsAuthenticationTypes.None);
        }

        #region Helpers

        private static async Task TestGetIsSupportedAsync(string[] wwwAuthHeaders, WindowsAuthenticationTypes expected)
        {
            var context = new TestCommandContext();
            var uri = new Uri("https://example.com");

            var wiaResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            foreach (string headerValue in wwwAuthHeaders)
            {
                wiaResponse.Headers.WwwAuthenticate.ParseAdd(headerValue);
            }

            var httpHandler = new TestHttpMessageHandler {ThrowOnUnexpectedRequest = true};
            httpHandler.Setup(HttpMethod.Head, uri, wiaResponse);

            context.HttpClientFactory.MessageHandler = httpHandler;
            var wiaAuth = new WindowsIntegratedAuthentication(context);

            WindowsAuthenticationTypes actual = await wiaAuth.GetAuthenticationTypesAsync(uri);

            Assert.Equal(expected, actual);
            httpHandler.AssertRequest(HttpMethod.Head, uri, expectedNumberOfCalls: 1);
        }

        #endregion
    }
}
