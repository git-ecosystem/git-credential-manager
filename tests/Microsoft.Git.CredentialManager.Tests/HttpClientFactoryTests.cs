// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Collections.Generic;
using System.Net.Http;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests
{
    public class HttpClientFactoryTests
    {
        [Fact]
        public void HttpClientFactory_GetClient_SetsDefaultHeaders()
        {
            var factory = new HttpClientFactory();

            HttpClient client = factory.GetClient();

            Assert.NotNull(client);
            Assert.Equal(Constants.GetHttpUserAgent(), client.DefaultRequestHeaders.UserAgent.ToString());
            Assert.True(client.DefaultRequestHeaders.CacheControl.NoCache);
        }

        [Fact]
        public void HttpClientFactory_GetClient_BearerToken_SetsAuthorizationBearerTokenHeader()
        {
            const string bearerToken = "letmein123";

            var factory = new HttpClientFactory();

            HttpClient client = factory.GetClient(bearerToken);

            Assert.NotNull(client);
            Assert.NotNull(client.DefaultRequestHeaders.Authorization);
            Assert.Equal("Bearer", client.DefaultRequestHeaders.Authorization.Scheme);
            Assert.Equal(bearerToken, client.DefaultRequestHeaders.Authorization.Parameter);
        }

        [Fact]
        public void HttpClientFactory_GetClient_CustomHeaders_SetsHeaders()
        {
            var customHeaders = new Dictionary<string, IEnumerable<string>>
            {
                ["header0"] = new string[0],
                ["header1"] = new []{ "first-value" },
                ["header2"] = new []{ "first-value", "second-value"},
                ["header3"] = new []{ "first-value", "second-value", "third-value"},
            };

            var factory = new HttpClientFactory();

            HttpClient client = factory.GetClient(customHeaders);

            Assert.NotNull(client);
            Assert.False(client.DefaultRequestHeaders.Contains("header0"));
            Assert.True(client.DefaultRequestHeaders.Contains("header1"));
            Assert.True(client.DefaultRequestHeaders.Contains("header2"));
            Assert.True(client.DefaultRequestHeaders.Contains("header3"));
            Assert.Equal(customHeaders["header1"], client.DefaultRequestHeaders.GetValues("header1"));
            Assert.Equal(customHeaders["header2"], client.DefaultRequestHeaders.GetValues("header2"));
            Assert.Equal(customHeaders["header3"], client.DefaultRequestHeaders.GetValues("header3"));
        }
    }
}
