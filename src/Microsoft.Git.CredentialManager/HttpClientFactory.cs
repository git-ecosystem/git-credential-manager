// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Microsoft.Git.CredentialManager
{
    /// <summary>
    /// Acquires <see cref="HttpClient"/>s that have been configured for use in Git Credential Manager.
    /// </summary>
    public interface IHttpClientFactory
    {
        /// <summary>
        /// Get an instance of <see cref="HttpClient"/> with default request headers set.
        /// </summary>
        /// <returns>Client with default headers.</returns>
        HttpClient GetClient();
    }

    public class HttpClientFactory : IHttpClientFactory
    {
        public HttpClient GetClient()
        {
            var client = new HttpClient();

            // Add default headers
            client.DefaultRequestHeaders.UserAgent.ParseAdd(Constants.GetHttpUserAgent());
            client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true
            };

            return client;
        }
    }

    public static class HttpClientFactoryExtensions
    {
        /// <summary>
        /// Get an instance of <see cref="HttpClient"/> with default headers and a bearer token based authorization header.
        /// </summary>
        /// <param name="factory"><see cref="IHttpClientFactory"/></param>
        /// <param name="bearerToken">Bearer token value</param>
        /// <returns>Client with default and bearer token headers.</returns>
        public static HttpClient GetClient(this IHttpClientFactory factory, string bearerToken)
        {
            var client = factory.GetClient();

            if (bearerToken != null)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            }

            return client;
        }

        /// <summary>
        /// Get an instance of <see cref="HttpClient"/> with default headers and additional custom headers.
        /// </summary>
        /// <param name="factory"><see cref="IHttpClientFactory"/></param>
        /// <param name="headers">Custom HTTP headers to set on the client.</param>
        /// <returns>Client with default and custom headers.</returns>
        public static HttpClient GetClient(this IHttpClientFactory factory, IDictionary<string, IEnumerable<string>> headers)
        {
            var client = factory.GetClient();

            foreach (var header in headers)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            return client;
        }
    }
}
