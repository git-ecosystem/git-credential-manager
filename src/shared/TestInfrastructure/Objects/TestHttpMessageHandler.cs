// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager.Authentication;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests.Objects
{
    public class TestHttpMessageHandler : HttpMessageHandler
    {
        public delegate HttpResponseMessage RequestHandler(HttpRequestMessage request);

        private readonly IDictionary<(HttpMethod method, Uri uri), RequestHandler> _handlers =
                      new Dictionary<(HttpMethod, Uri), RequestHandler>();

        private readonly IDictionary<(HttpMethod method, Uri uri), int> _requestCounts =
                      new Dictionary<(HttpMethod, Uri), int>();

        public bool ThrowOnUnexpectedRequest { get; set; }
        public bool SimulateNoNetwork { get; set; }

        public void Setup(HttpMethod method, Uri uri, RequestHandler handler)
        {
            _handlers[(method, uri)] = handler;
        }

        public void Setup(HttpMethod method, Uri uri, HttpResponseMessage responseMessage)
        {
            _handlers[(method, uri)] = _ => responseMessage;
        }

        public void Setup(HttpMethod method, Uri uri, HttpStatusCode responseCode)
        {
            Setup(method, uri, responseCode, string.Empty);
        }

        public void Setup(HttpMethod method, Uri uri, HttpStatusCode responseCode, string content)
        {
            Setup(method, uri, new HttpResponseMessage(responseCode){Content = new StringContent(content)});
        }

        public void AssertRequest(HttpMethod method, Uri uri, int expectedNumberOfCalls)
        {
            int numCalls;
            if (!_requestCounts.TryGetValue((method, uri), out numCalls))
            {
                numCalls = 0;
            }

            Assert.Equal(expectedNumberOfCalls, numCalls);
        }

        #region HttpMessageHandler

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            (HttpMethod method, Uri uri) requestKey = (request.Method, request.RequestUri);

            IncrementRequestCount(requestKey);

            if (SimulateNoNetwork)
            {
                throw new HttpRequestException("Simulated no network");
            }

            foreach (var kvp in _handlers)
            {
                if (kvp.Key == requestKey)
                {
                    return Task.FromResult(kvp.Value(request));
                }
            }

            if (ThrowOnUnexpectedRequest)
            {
                throw new Exception($"No handler configured for the request '{request.Method} {request.RequestUri}'");
            }
            else
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }
        }

        #endregion

        private void IncrementRequestCount((HttpMethod, Uri) requestKey)
        {
            if (!_requestCounts.ContainsKey(requestKey))
            {
                _requestCounts[requestKey] = 0;
            }
            _requestCounts[requestKey]++;
        }
    }
}
