using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager.Diagnostics
{
    public class NetworkingDiagnostic : Diagnostic
    {
        private readonly IHttpClientFactory _httpFactory;
        private const string TestHttpUri = "http://example.com";
        private const string TestHttpsUri = "https://example.com";

        public NetworkingDiagnostic(IHttpClientFactory httpFactory)
            : base("Networking")
        {
            EnsureArgument.NotNull(httpFactory, nameof(httpFactory));

            _httpFactory = httpFactory;
        }

        protected override async Task<bool> RunInternalAsync(StringBuilder log, IList<string> additionalFiles)
        {
            log.AppendLine("Checking basic networking and HTTP stack...");
            log.Append("Creating HTTP client...");
            using var rawHttpClient = new HttpClient();
            log.AppendLine(" OK");

            if (!await RunTestAsync(log, rawHttpClient)) return false;

            log.AppendLine("Testing with IHttpClientFactory created HTTP client...");
            log.Append("Creating HTTP client...");
            using var contextHttpClient = _httpFactory.CreateClient();
            log.AppendLine(" OK");

            return await RunTestAsync(log, rawHttpClient);
        }

        private static async Task<bool> RunTestAsync(StringBuilder log, HttpClient httpClient)
        {
            bool hasNetwork = NetworkInterface.GetIsNetworkAvailable();
            log.AppendLine($"IsNetworkAvailable: {hasNetwork}");

            log.Append($"Sending HEAD request to {TestHttpUri}...");
            using var httpResponse = await httpClient.HeadAsync(TestHttpUri);
            log.AppendLine(" OK");

            log.Append($"Sending HEAD request to {TestHttpsUri}...");
            using var httpsResponse = await httpClient.HeadAsync(TestHttpsUri);
            log.AppendLine(" OK");

            log.Append("Acquiring free TCP port...");
            var tcpListener = new TcpListener(IPAddress.Loopback, 0);
            int tcpPort;
            try
            {
                tcpListener.Start();
                tcpPort = ((IPEndPoint) tcpListener.LocalEndpoint).Port;
                log.AppendLine(" OK");
            }
            finally
            {
                tcpListener.Stop();
            }

            if (tcpPort <= 0)
            {
                log.AppendLine("Failed to acquire local TCP port - cannot test local HTTP loopback connections!");
                return false;
            }

            log.AppendLine("Testing local HTTP loopback connections...");

            const string responseContent = "Hello, GCM!";
            byte[] responseData = Encoding.UTF8.GetBytes(responseContent);

            var localAddress = $"http://localhost:{tcpPort}/";
            log.Append($"Creating new HTTP listener for {localAddress}...");
            var httpListener = new HttpListener {Prefixes = {localAddress}};
            httpListener.Start();
            log.AppendLine(" OK");

            Task<HttpListenerContext> listenContextTask = httpListener.GetContextAsync();
            Task<HttpResponseMessage> localResponseTask = httpClient.GetAsync(localAddress);

            log.Append("Waiting for loopback connection...");
            HttpListenerContext listenContext = await listenContextTask;
            log.AppendLine(" OK");

            log.Append("Writing response...");
            listenContext.Response.ContentLength64 = responseData.Length;
            listenContext.Response.OutputStream.Write(responseData, 0, responseData.Length);
            listenContext.Response.Close();
            log.AppendLine(" OK");

            log.Append("Waiting for response data...");
            using HttpResponseMessage localResponse = await localResponseTask;
            byte[] actualResponseData = await localResponse.Content.ReadAsByteArrayAsync();
            string actualResponseContent = Encoding.UTF8.GetString(actualResponseData);
            log.AppendLine(" OK");

            if (!StringComparer.Ordinal.Equals(responseContent, actualResponseContent))
            {
                log.AppendLine("Loopback connection data did not match!");
                log.AppendLine($"Expected: {responseContent}");
                log.AppendLine($"Actual: {actualResponseContent}");
                return false;
            }

            log.AppendLine("Loopback connection data OK");

            return true;
        }
    }
}
