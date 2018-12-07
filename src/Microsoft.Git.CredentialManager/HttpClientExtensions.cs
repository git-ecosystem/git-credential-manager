// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager
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
            return client.HeadAsync(requestUri, HttpCompletionOption.ResponseContentRead);
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
            return client.HeadAsync(requestUri, HttpCompletionOption.ResponseContentRead, cancellationToken);
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
    }
}
