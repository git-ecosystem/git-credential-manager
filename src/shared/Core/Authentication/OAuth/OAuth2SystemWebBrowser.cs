using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace GitCredentialManager.Authentication.OAuth
{
    public class OAuth2WebBrowserOptions
    {
        internal const string DefaultSuccessHtml = @"<!DOCTYPE html><html><head>
<style>body{font-family:sans-serif;}dt{font-weight:bold;}dd{margin-bottom:10px;}</style>
<title>Authentication successful</title></head>
<body><h1>Authentication successful</h1><p>You can now close this page.</p></body>
</html>";
        internal const string DefaultFailureHtmlFormat = @"<!DOCTYPE html><html><head>
<style>body{{font-family:sans-serif;}}dt{{font-weight:bold;}}dd{{margin-bottom:10px;}}</style>
<title>Authentication failed</title></head>
<body><h1>Authentication failed</h1><dl>
<dt>Error:</dt><dd>{0}</dd>
<dt>Description:</dt><dd>{1}</dd>
<dt>URL:</dt><dd>{2}</dd>
</dl></body></html>";

        public string SuccessResponseHtml { get; set; }
        public string FailureResponseHtmlFormat { get; set; }

        public Uri SuccessRedirect { get; set; }
        public Uri FailureRedirectFormat { get; set; }
    }

    public class OAuth2SystemWebBrowser : IOAuth2WebBrowser
    {
        private readonly IEnvironment _environment;
        private readonly OAuth2WebBrowserOptions _options;

        public OAuth2SystemWebBrowser(IEnvironment environment, OAuth2WebBrowserOptions options)
        {
            EnsureArgument.NotNull(environment, nameof(environment));
            EnsureArgument.NotNull(options, nameof(options));

            _environment = environment;
            _options = options;
        }

        public Uri UpdateRedirectUri(Uri uri)
        {
            if (!uri.IsLoopback)
            {
                throw new ArgumentException("Only localhost is supported as a redirect URI.", nameof(uri));
            }

            // If a port has been specified use it, otherwise find a free one
            if (uri.IsDefaultPort)
            {
                int port = GetFreeTcpPort();
                return new UriBuilder(uri) {Port = port}.Uri;
            }

            return uri;
        }

        public async Task<Uri> GetAuthenticationCodeAsync(Uri authorizationUri, Uri redirectUri, CancellationToken ct)
        {
            if (!redirectUri.IsLoopback)
            {
                throw new ArgumentException("Only localhost is supported as a redirect URI.", nameof(redirectUri));
            }

            Task<Uri> interceptTask = InterceptRequestsAsync(redirectUri, ct);

            BrowserUtils.OpenDefaultBrowser(_environment, authorizationUri);

            return await interceptTask;
        }

        private async Task<Uri> InterceptRequestsAsync(Uri listenUri, CancellationToken ct)
        {
            // Create a TaskCompletionSource which completes when we're asked to cancel.
            // We can then await the this task together with other tasks that don't take a
            // CancellationToken and exit the method quickly when cancelled.
            var tcs = new TaskCompletionSource<Uri>();
            ct.Register(() => tcs.SetCanceled());

            // Prefixes must end with a '/'
            string prefix = listenUri.GetLeftPart(UriPartial.Path);
            if (!prefix.EndsWith("/"))
            {
                prefix += "/";
            }

            var listener = new HttpListener {Prefixes = {prefix}};
            listener.Start();

            try
            {
                Task<HttpListenerContext> contextTask = listener.GetContextAsync();
                Task<Uri> cancelTask = tcs.Task;

                Task completedTask = await Task.WhenAny(contextTask, tcs.Task);

                // Check if we 'completed' the context task or the cancellation task
                if (completedTask == cancelTask)
                {
                    // We were cancelled!
                    return await cancelTask;
                }

                // We intercepted a request!
                HttpListenerContext context = await contextTask;

                await HandleInterceptedRequestAsync(context.Request, context.Response);

                // Return the final intercepted URI
                return context.Request.Url;
            }
            finally
            {
                listener.Stop();
                listener.Close();
            }
        }

        private async Task HandleInterceptedRequestAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            IDictionary<string, string> queryParams = request.QueryString.ToDictionary(StringComparer.OrdinalIgnoreCase);

            // If we have an error value then the request failed and we should reply with a page containing the error information
            bool hasError = queryParams.TryGetValue(OAuth2Constants.AuthorizationGrantResponse.ErrorCodeParameter, out string errorCode);
            queryParams.TryGetValue(OAuth2Constants.AuthorizationGrantResponse.ErrorDescriptionParameter, out string errorDescription);
            queryParams.TryGetValue(OAuth2Constants.AuthorizationGrantResponse.ErrorUriParameter, out string errorUri);
            if (hasError)
            {
                string FormatError(string format)
                {
                    if (string.IsNullOrWhiteSpace(errorCode)) errorCode = "unknown";
                    if (string.IsNullOrWhiteSpace(errorDescription)) errorDescription = "Unknown error.";
                    if (string.IsNullOrWhiteSpace(errorUri)) errorUri = "none";
                    return string.Format(format, errorCode, errorDescription, errorUri);
                }

                // Prefer redirection options to raw HTML
                if (_options.FailureRedirectFormat != null)
                {
                    string failureUrl = FormatError(_options.FailureRedirectFormat.ToString());
                    response.Redirect(failureUrl);
                    response.Close();
                }
                else
                {
                    string failureHtml = FormatError(_options.FailureResponseHtmlFormat ?? OAuth2WebBrowserOptions.DefaultFailureHtmlFormat);
                    await response.WriteResponseAsync(failureHtml);
                    response.Close();
                }
            }
            else
            {
                // Prefer redirection options to raw HTML
                if (_options.SuccessRedirect != null)
                {
                    string successUrl = _options.SuccessRedirect.ToString();
                    response.Redirect(successUrl);
                    response.Close();
                }
                else
                {
                    string successHtml = _options.SuccessResponseHtml ?? OAuth2WebBrowserOptions.DefaultSuccessHtml;
                    await response.WriteResponseAsync(successHtml);
                    response.Close();
                }
            }
        }

        private static int GetFreeTcpPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);

            try
            {
                listener.Start();
                return ((IPEndPoint) listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }

    }
}
