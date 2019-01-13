// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Net.Http;
using System.Net.Http.Headers;

namespace Microsoft.Git.CredentialManager
{
    /// <summary>
    /// Constructs <see cref="HttpClient"/>s that have been configured for use in Git Credential Manager.
    /// </summary>
    public interface IHttpClientFactory
    {
        /// <summary>
        /// Get a new instance of <see cref="HttpClient"/> with default request headers set.
        /// </summary>
        /// <remarks>
        /// Callers should reuse instances of <see cref="HttpClient"/> returned from this method as long
        /// as they are needed, rather than repeatably call this method to create new ones.
        /// <para/>
        /// Creating a new <see cref="HttpClient"/> consumes one free socket which may not be released
        /// by the Operating System until sometime after the client is disposed, leading to possible free
        /// socket exhaustion.
        /// </remarks>
        /// <returns>New client instance with default headers.</returns>
        HttpClient CreateClient();
    }

    public class HttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient()
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
