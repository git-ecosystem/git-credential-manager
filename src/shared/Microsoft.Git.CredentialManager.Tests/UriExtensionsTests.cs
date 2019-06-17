// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Linq;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests
{
    public class UriExtensionsTests
    {
        [Theory]
        [InlineData("http://com")]
        [InlineData("http://example.com",
            "http://example.com")]
        [InlineData("http://foo.example.com",
            "http://foo.example.com", "http://example.com")]
        [InlineData("http://example.com/foo",
            "http://example.com/foo", "http://example.com")]
        [InlineData("http://example.com/foo/",
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
    }
}
