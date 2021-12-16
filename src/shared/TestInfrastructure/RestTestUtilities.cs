using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Xunit;

namespace GitCredentialManager.Tests
{
    public static class RestTestUtilities
    {
        public static void AssertBasicAuth(HttpRequestMessage request, string userName, string password)
        {
            string expectedBasicValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userName}:{password}"));

            AuthenticationHeaderValue authHeader = request.Headers.Authorization;
            Assert.NotNull(authHeader);
            Assert.Equal("Basic", authHeader.Scheme);
            Assert.Equal(expectedBasicValue, authHeader.Parameter);
        }

        public static void AssertBearerAuth(HttpRequestMessage request, string token)
        {
            AuthenticationHeaderValue authHeader = request.Headers.Authorization;
            Assert.NotNull(authHeader);
            Assert.Equal("Bearer", authHeader.Scheme);
            Assert.Equal(token, authHeader.Parameter);
        }
    }
}