// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
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
            // Initialize a new HttpClient
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
}
