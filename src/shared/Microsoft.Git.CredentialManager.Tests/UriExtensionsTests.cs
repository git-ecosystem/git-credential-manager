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
    }
}
