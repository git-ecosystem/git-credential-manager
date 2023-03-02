using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace GitCredentialManager.Tests
{
    public class UriExtensionsTests
    {
        [Fact]
        public void UriExtensions_GetQueryParameters()
        {
            var uri = new Uri("https://example.com/foo/bar?q1=value1&q2=value%20with%20spaces&key%20with%20spaces=value3");

            IDictionary<string, string> result = uri.GetQueryParameters();

            Assert.Equal(3, result.Count);

            Assert.True(result.TryGetValue("q1", out string value1));
            Assert.Equal("value1", value1);

            Assert.True(result.TryGetValue("q2", out string value2));
            Assert.Equal("value with spaces", value2);

            Assert.True(result.TryGetValue("key with spaces", out string value3));
            Assert.Equal("value3", value3);
        }

        [Theory]
        [InlineData("http://hostname", "http://hostname")]
        [InlineData("http://example.com",
            "http://example.com")]
        [InlineData("http://hostname:7990", "http://hostname:7990")]
        [InlineData("http://foo.example.com",
            "http://foo.example.com", "http://example.com")]
        [InlineData("http://example.com/foo",
            "http://example.com/foo", "http://example.com")]
        [InlineData("http://example.com/foo?query=true#fragment",
            "http://example.com/foo", "http://example.com")]
        [InlineData("http://buzz.foo.example.com/bar/baz",
            "http://buzz.foo.example.com/bar/baz", "http://buzz.foo.example.com/bar", "http://buzz.foo.example.com", "http://foo.example.com", "http://example.com")]
        public void UriExtensions_GetGitConfigurationScopes(string start, params string[] expected)
        {
            var startUri = new Uri(start);
            string[] actual = UriExtensions.GetGitConfigurationScopes(startUri).ToArray();
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("http://example.com", false, null, null)]

        [InlineData("http://john.doe:password123@example.com",
            true, "john.doe", "password123")]

        [InlineData("http://john.doe@example.com",
            true, "john.doe", null)]

        [InlineData("http://john.doe:@example.com",
            true, "john.doe", "")]

        [InlineData("http://:password123@example.com",
            true, "", "password123")]

        [InlineData("http://john.doe:::password123@example.com",
            true, "john.doe", "::password123")]

        [InlineData("http://john%20doe:password%20123@example.com",
            true, "john doe", "password 123")]
        public void UriExtensions_GetUserInfo(string input, bool expectedResult, string expectedUser, string expectedPass)
        {
            var uri = new Uri(input);

            bool actualResult = UriExtensions.TryGetUserInfo(uri, out string actualUser, out string actualPass);

            Assert.Equal(expectedResult, actualResult);
            Assert.Equal(expectedUser, actualUser);
            Assert.Equal(expectedPass, actualPass);
        }

        [Theory]
        [InlineData("http://example.com", "http://example.com")]
        [InlineData("http://john.doe:password123@example.com", "http://example.com")]
        [InlineData("http://john.doe@example.com", "http://example.com")]
        [InlineData("http://john.doe:@example.com", "http://example.com")]
        [InlineData("http://:password123@example.com", "http://example.com")]
        [InlineData("http://john.doe:::password123@example.com", "http://example.com")]
        [InlineData("http://john%20doe:password%20123@example.com", "http://example.com")]
        public void UriExtensions_WithoutUserInfo(string input, string expected)
        {
            var uri = new Uri(input);

            Uri result = UriExtensions.WithoutUserInfo(uri);

            Assert.Equal(expected, result.ToString().TrimEnd('/'));
        }
    }
}
