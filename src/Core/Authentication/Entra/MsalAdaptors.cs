using System.Net.Http;
using Microsoft.Identity.Client;

namespace GitCredentialManager.Authentication.Entra
{
    internal class MsalHttpClientFactoryAdaptor : IMsalHttpClientFactory
    {
        private readonly IHttpClientFactory _factory;
        private HttpClient _instance;

        public MsalHttpClientFactoryAdaptor(IHttpClientFactory factory)
        {
            EnsureArgument.NotNull(factory, nameof(factory));

            _factory = factory;
        }

        public HttpClient GetHttpClient()
        {
            // MSAL calls this method each time it wants to use an HTTP client.
            // We ensure we only create a single instance to avoid socket exhaustion.
            return _instance ?? (_instance = _factory.CreateClient());
        }
    }
}
