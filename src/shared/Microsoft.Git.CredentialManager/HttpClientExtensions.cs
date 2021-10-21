using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace GitCredentialManager
{
    public static class HttpClientExtensions
    {
        #region HeadAsync

        public static Task<HttpResponseMessage> HeadAsync(this HttpClient client, string requestUri)
        {
            return client.HeadAsync(new Uri(requestUri));
        }

        public static Task<HttpResponseMessage> HeadAsync(this HttpClient client, Uri requestUri)
        {
            return client.HeadAsync(requestUri, HttpCompletionOption.ResponseHeadersRead);
        }

        public static Task<HttpResponseMessage> HeadAsync(this HttpClient client, string requestUri, HttpCompletionOption completionOption)
        {
            return client.HeadAsync(new Uri(requestUri), completionOption);
        }

        public static Task<HttpResponseMessage> HeadAsync(this HttpClient client, Uri requestUri, HttpCompletionOption completionOption)
        {
            return client.HeadAsync(requestUri, completionOption, CancellationToken.None);
        }

        public static Task<HttpResponseMessage> HeadAsync(this HttpClient client, string requestUri, CancellationToken cancellationToken)
        {
            return client.HeadAsync(new Uri(requestUri), cancellationToken);
        }

        public static Task<HttpResponseMessage> HeadAsync(this HttpClient client, Uri requestUri, CancellationToken cancellationToken)
        {
            return client.HeadAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }

        public static Task<HttpResponseMessage> HeadAsync(this HttpClient client, string requestUri, HttpCompletionOption completionOption, CancellationToken cancellationToken)
        {
            return client.HeadAsync(new Uri(requestUri), completionOption, cancellationToken);
        }

        public static Task<HttpResponseMessage> HeadAsync(this HttpClient client, Uri requestUri, HttpCompletionOption completionOption, CancellationToken cancellationToken)
        {
            return client.SendAsync(new HttpRequestMessage(HttpMethod.Head, requestUri), completionOption, cancellationToken);
        }

        #endregion

        #region SendAsync with content and headers

        public static Task<HttpResponseMessage> SendAsync(this HttpClient client,
            HttpMethod method,
            Uri requestUri,
            IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = null,
            HttpContent content = null)
        {
            var request = new HttpRequestMessage(method, requestUri)
            {
                Content = content
            };

            if (!(headers is null))
            {
                foreach (var kvp in headers)
                {
                    request.Headers.Add(kvp.Key, kvp.Value);
                }
            }

            return client.SendAsync(request);
        }

        #endregion
    }
}
