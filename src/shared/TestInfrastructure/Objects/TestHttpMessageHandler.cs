using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace GitCredentialManager.Tests.Objects
{
    public class TestHttpMessageHandler : HttpMessageHandler
    {
        public delegate HttpResponseMessage RequestHandler(HttpRequestMessage request);

        public delegate Task<HttpResponseMessage> AsyncRequestHandler(HttpRequestMessage request);

        private readonly IDictionary<(HttpMethod method, Uri uri), AsyncRequestHandler> _handlers =
                      new Dictionary<(HttpMethod, Uri), AsyncRequestHandler>();

        private readonly IDictionary<(HttpMethod method, Uri uri), int> _requestCounts =
                      new Dictionary<(HttpMethod, Uri), int>();

        public bool ThrowOnUnexpectedRequest { get; set; }
        public bool SimulateNoNetwork { get; set; }

        public bool SimulatePrimaryUriFailure { get; set; }

        public IDictionary<(HttpMethod method, Uri uri), int> RequestCounts => _requestCounts;

        public void Setup(HttpMethod method, Uri uri, AsyncRequestHandler handler)
        {
            _handlers[CreateRequestKey(method, uri)] = handler;
        }

        public void Setup(HttpMethod method, Uri uri, RequestHandler handler)
        {
            Setup(method, uri, x => Task.FromResult(handler(x)));
        }

        public void Setup(HttpMethod method, Uri uri, HttpResponseMessage responseMessage)
        {
            Setup(method, uri, _ => responseMessage);
        }

        public void Setup(HttpMethod method, Uri uri, HttpStatusCode responseCode, string content)
        {
            Setup(method, uri, new HttpResponseMessage(responseCode){Content = new StringContent(content)});
        }

        public void Setup(HttpMethod method, Uri uri, HttpStatusCode responseCode)
        {
            Setup(method, uri, responseCode, string.Empty);
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

        public void AssertNoRequests()
        {
            Assert.Equal(0, _requestCounts.Count);
        }

        #region HttpMessageHandler

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Build the request key to match against registered handlers
            (HttpMethod method, Uri uri) requestKey = CreateRequestKey(request.Method, request.RequestUri);

            IncrementRequestCount(requestKey);

            if (SimulateNoNetwork)
            {
                throw new HttpRequestException("Simulated no network");
            }

            if (SimulatePrimaryUriFailure && request.RequestUri != null  &&
                request.RequestUri.ToString().Equals("http://example.com/"))
            {
                throw new HttpRequestException("Simulated http failure.");
            }

            foreach (var kvp in _handlers)
            {
                if (kvp.Key == requestKey)
                {
                    return await kvp.Value(request);
                }
            }

            if (ThrowOnUnexpectedRequest)
            {
                throw new Exception($"No handler configured for the request '{request.Method} {request.RequestUri}'");
            }
            else
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        }

        #endregion

        private static (HttpMethod Method, Uri requestUri) CreateRequestKey(HttpMethod method, Uri uri)
        {
            // Trim the query and fragment
            var normalizedUri = new Uri(uri.GetLeftPart(UriPartial.Path));

            return (method, normalizedUri);
        }

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
