using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GitCredentialManager.Diagnostics
{
    public class NetworkingDiagnostic : Diagnostic
    {
        private const string TestHttpUri = "http://example.com";
        private const string TestHttpUriFallback = "http://httpforever.com";
        private const string TestHttpsUri = "https://example.com";

        public NetworkingDiagnostic(ICommandContext commandContext)
            : base("Networking", commandContext)
        { }

        protected override async Task RunInternalAsync(IDiagnosticReporter reporter)
        {
            reporter.ReportProgress("Checking networking and HTTP stack");
            reporter.ReportProgress("Creating HTTP client");
            using var httpClient = Context.HttpClientFactory.CreateClient();

            bool hasNetwork = NetworkInterface.GetIsNetworkAvailable();
            reporter.ReportInfo($"IsNetworkAvailable: {hasNetwork}");

            await SendHttpRequestAsync(reporter, httpClient);

            reporter.ReportProgress($"Sending HEAD request to {TestHttpsUri}");
            using var httpsResponse = await httpClient.HeadAsync(TestHttpsUri);

            reporter.ReportProgress("Acquiring free TCP port");
            var tcpListener = new TcpListener(IPAddress.Loopback, 0);
            int tcpPort;
            try
            {
                tcpListener.Start();
                tcpPort = ((IPEndPoint) tcpListener.LocalEndpoint).Port;
            }
            finally
            {
                tcpListener.Stop();
            }

            if (tcpPort <= 0)
            {
                reporter.ReportError("Failed to acquire local TCP port - cannot test local HTTP loopback connections!");
                return;
            }

            reporter.ReportInfo($"Got port {tcpPort}");
            reporter.ReportProgress("Testing local HTTP loopback connections...");

            const string responseContent = "Hello, GCM!";
            byte[] responseData = Encoding.UTF8.GetBytes(responseContent);

            var localAddress = $"http://localhost:{tcpPort}/";
            reporter.ReportProgress($"Creating new HTTP listener for {localAddress}");
            var httpListener = new HttpListener {Prefixes = {localAddress}};
            httpListener.Start();

            Task<HttpListenerContext> listenContextTask = httpListener.GetContextAsync();
            Task<HttpResponseMessage> localResponseTask = httpClient.GetAsync(localAddress);

            reporter.ReportProgress("Waiting for loopback connection");
            HttpListenerContext listenContext = await listenContextTask;

            reporter.ReportProgress("Writing response");
            listenContext.Response.ContentLength64 = responseData.Length;
            listenContext.Response.OutputStream.Write(responseData, 0, responseData.Length);
            listenContext.Response.Close();

            reporter.ReportProgress("Waiting for response data");
            using HttpResponseMessage localResponse = await localResponseTask;
            byte[] actualResponseData = await localResponse.Content.ReadAsByteArrayAsync();
            string actualResponseContent = Encoding.UTF8.GetString(actualResponseData);

            if (!StringComparer.Ordinal.Equals(responseContent, actualResponseContent))
            {
                reporter.ReportError("Loopback connection data did not match!");
                reporter.ReportError($"Expected: {responseContent}");
                reporter.ReportError($"Actual: {actualResponseContent}");
                return;
            }

            reporter.ReportInfo("Loopback connection data OK");
        }

        internal /* For testing purposes */ async Task SendHttpRequestAsync(
            IDiagnosticReporter reporter, HttpClient httpClient)
        {
            foreach (var uri in new List<string> { TestHttpUri, TestHttpUriFallback })
            {
                try
                {
                    reporter.ReportProgress($"Sending HEAD request to {uri}");
                    using var httpResponse = await httpClient.HeadAsync(uri);
                    break;
                }
                catch (HttpRequestException)
                {
                    reporter.ReportWarning("HEAD request failed");
                }
            }
        }
    }
}
