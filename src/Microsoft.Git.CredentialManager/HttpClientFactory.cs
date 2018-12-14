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
        private HttpClient _client;

        public HttpClient GetClient()
        {
            if (_client is null)
            {
                // Initialize a new HttpClient
                _client = new HttpClient();

                // Add default headers
                _client.DefaultRequestHeaders.UserAgent.ParseAdd(Constants.GetHttpUserAgent());
                _client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
                {
                    NoCache = true
                };
            }

            return _client;
        }
    }
}
