using System.Net.Http;
using Xunit;

namespace GitCredentialManager.Tests
{
    public class HttpRequestExtensionsTests
    {
        [Fact]
        public void HttpRequestExtensions_AddBasicAuthenticationHeader_ComplexUserPass_ReturnsCorrectString()
        {
            const string expected = "aGVsbG8tbXlfbmFtZSBpczpqb2huLmRvZTp0aGlzIWlzQVA0U1NXMFJEOiB3aXRoPyBfbG90cyBvZi8gY2hhcnM=";
            const string testUserName = "hello-my_name is:john.doe";
            const string testPassword = "this!isAP4SSW0RD: with? _lots of/ chars"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]

            TestAddBasicAuthenticationHeader(testUserName, testPassword, expected);
        }

        [Fact]
        public void HttpRequestExtensions_AddBasicAuthenticationHeader_EmptyUserName_ReturnsCorrectString()
        {
            const string expected = "OmxldG1laW4xMjM=";
            const string testUserName = "";
            const string testPassword = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]

            TestAddBasicAuthenticationHeader(testUserName, testPassword, expected);
        }

        [Fact]
        public void HttpRequestExtensions_AddBasicAuthenticationHeader_EmptyPassword_ReturnsCorrectString()
        {
            const string expected = "am9obi5kb2U6";
            const string testUserName = "john.doe";
            const string testPassword = ""; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]

            TestAddBasicAuthenticationHeader(testUserName, testPassword, expected);
        }

        [Fact]
        public void HttpRequestExtensions_AddBasicAuthenticationHeader_EmptyCredential_ReturnsCorrectString()
        {
            const string expected = "Og==";
            const string testUserName = "";
            const string testPassword = ""; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]

            TestAddBasicAuthenticationHeader(testUserName, testPassword, expected);
        }

        private static void TestAddBasicAuthenticationHeader(string userName, string password, string expectedParameterValue)
        {
            var message = new HttpRequestMessage();
            message.AddBasicAuthenticationHeader(userName, password);

            var authHeader = message.Headers.Authorization;
            Assert.NotNull(authHeader);
            Assert.Equal(Constants.Http.WwwAuthenticateBasicScheme, authHeader.Scheme);
            Assert.Equal(expectedParameterValue, authHeader.Parameter);
        }
    }
}
