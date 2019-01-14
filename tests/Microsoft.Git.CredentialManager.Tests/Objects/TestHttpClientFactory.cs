// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Net.Http;

namespace Microsoft.Git.CredentialManager.Tests.Objects
{
    public class TestHttpClientFactory : IHttpClientFactory
    {
        public HttpMessageHandler MessageHandler { get; set; } = new TestHttpMessageHandler();

        #region IHttpClientFactory

        HttpClient IHttpClientFactory.CreateClient()
        {
            return new HttpClient(MessageHandler);
        }

        #endregion
    }
}
