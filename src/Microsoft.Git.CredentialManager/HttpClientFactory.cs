using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Microsoft.Git.CredentialManager
{
    public interface IHttpClientFactory
    {
        HttpClient GetClient();
    }

    public class HttpClientFactory : IHttpClientFactory
    {
        public HttpClient GetClient()
        {
            var client = new HttpClient();

            // Add default headers
            client.DefaultRequestHeaders.UserAgent.ParseAdd(Constants.GetHttpUserAgent());

            return client;
        }
    }

    public static class HttpClientFactoryExtensions
    {
        public static HttpClient GetClient(this IHttpClientFactory factory, string bearerToken)
        {
            var client = factory.GetClient();

            if (bearerToken != null)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            }

            return client;
        }

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
