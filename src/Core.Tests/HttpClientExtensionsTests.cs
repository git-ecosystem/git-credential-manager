using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace GitCredentialManager.Tests
{
    public class HttpClientExtensionsTests
    {
        [Fact]
        public async Task HttpClientExtensions_SendAsync_SendsRequestMessage()
        {
            var method = HttpMethod.Get;
            var uri = new Uri("http://example.com");

            var httpHandler = new TestHttpMessageHandler{ThrowOnUnexpectedRequest = true};
            httpHandler.Setup(method, uri, request =>
            {
                Assert.Equal(method, request.Method);
                Assert.Equal(uri, request.RequestUri);

                return new HttpResponseMessage();
            });

            var httpClient = new HttpClient(httpHandler);

            await HttpClientExtensions.SendAsync(httpClient, method, uri);
        }

        [Fact]
        public async Task HttpClientExtensions_SendAsync_Content_SetsContent()
        {
            var method = HttpMethod.Get;
            var uri = new Uri("http://example.com");

            var expectedContent = new StringContent("foobar");

            var httpHandler = new TestHttpMessageHandler{ThrowOnUnexpectedRequest = true};
            httpHandler.Setup(method, uri, request =>
            {
                Assert.Same(expectedContent,request.Content);

                return new HttpResponseMessage();
            });

            var httpClient = new HttpClient(httpHandler);

            await HttpClientExtensions.SendAsync(httpClient, method, uri, null, expectedContent);
        }

        [Fact]
        public async Task HttpClientExtensions_SendAsync_Headers_SetsHeaders()
        {
            var method = HttpMethod.Get;
            var uri = new Uri("http://example.com");

            var customHeaders = new Dictionary<string, IEnumerable<string>>
            {
                ["header0"] = new string[0],
                ["header1"] = new []{ "first-value" },
                ["header2"] = new []{ "first-value", "second-value"},
                ["header3"] = new []{ "first-value", "second-value", "third-value"},
            };

            var httpHandler = new TestHttpMessageHandler{ThrowOnUnexpectedRequest = true};
            httpHandler.Setup(method, uri, request =>
            {
                Assert.False(request.Headers.Contains("header0"));
                Assert.True(request.Headers.Contains("header1"));
                Assert.True(request.Headers.Contains("header2"));
                Assert.True(request.Headers.Contains("header3"));
                Assert.Equal(customHeaders["header1"], request.Headers.GetValues("header1"));
                Assert.Equal(customHeaders["header2"], request.Headers.GetValues("header2"));
                Assert.Equal(customHeaders["header3"], request.Headers.GetValues("header3"));

                return new HttpResponseMessage();
            });

            var httpClient = new HttpClient(httpHandler);

            await HttpClientExtensions.SendAsync(httpClient, method, uri, customHeaders);
        }
    }
}
