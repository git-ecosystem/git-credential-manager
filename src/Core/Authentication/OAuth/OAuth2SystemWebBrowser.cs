using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitCredentialManager.Authentication.OAuth
{
    public class OAuth2WebBrowserOptions
    {
        internal const string DefaultSuccessHtml = @"<!DOCTYPE html><html><head>
<meta name=""color-scheme"" content=""light dark"">
<style>body{font-family:sans-serif;}dt{font-weight:bold;}dd{margin-bottom:10px;}
@media (prefers-color-scheme: dark){body{background:#0d1117;color:#c9d1d9;}a{color:#58a6ff;}}</style>
<title>Authentication successful</title></head>
<body><h1>Authentication successful</h1><p>You can now close this page.</p></body>
</html>";
        internal const string DefaultFailureHtmlFormat = @"<!DOCTYPE html><html><head>
<meta name=""color-scheme"" content=""light dark"">
<style>body{{font-family:sans-serif;}}dt{{font-weight:bold;}}dd{{margin-bottom:10px;}}
@media (prefers-color-scheme: dark){{body{{background:#0d1117;color:#c9d1d9;}}a{{color:#58a6ff;}}}}</style>
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
        // Served during the fragment response flow. The authorization parameters live in the
        // URI fragment, which user agents do not transmit to the server, so we reissue them as
        // a form POST to the redirect URI - keeping them out of the URL (and thus out of
        // browser history and server logs) and letting the listener read them from the body.
        private const string FragmentFormPostHtml = @"<!DOCTYPE html><html><head>
<meta name=""color-scheme"" content=""light dark""><title>Authenticating...</title></head>
<body><form id=""gcm-fragment-form"" method=""POST""></form><script>
(function () {
  var hash = window.location.hash;
  if (!hash || hash.length < 2) { return; }
  var data = hash.charAt(0) === '#' ? hash.substring(1) : hash;
  var form = document.getElementById('gcm-fragment-form');
  form.action = window.location.pathname;
  data.split('&').forEach(function (pair) {
    if (!pair) { return; }
    var eq = pair.indexOf('=');
    var name = eq < 0 ? pair : pair.substring(0, eq);
    var value = eq < 0 ? '' : pair.substring(eq + 1);
    var input = document.createElement('input');
    input.type = 'hidden';
    input.name = decodeURIComponent(name.replace(/\+/g, ' '));
    input.value = decodeURIComponent(value.replace(/\+/g, ' '));
    form.appendChild(input);
  });
  form.submit();
})();
</script></body></html>";

        private readonly ISessionManager _sessionManager;
        private readonly OAuth2WebBrowserOptions _options;

        public OAuth2SystemWebBrowser(ISessionManager sessionManager, OAuth2WebBrowserOptions options)
        {
            EnsureArgument.NotNull(sessionManager, nameof(sessionManager));
            EnsureArgument.NotNull(options, nameof(options));

            _sessionManager = sessionManager;
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

        public async Task<IDictionary<string, string>> GetAuthenticationResponseAsync(
            Uri authorizationUri, Uri redirectUri, OAuth2ResponseMode responseMode, CancellationToken ct)
        {
            if (!redirectUri.IsLoopback)
            {
                throw new ArgumentException("Only localhost is supported as a redirect URI.", nameof(redirectUri));
            }

            Task<IDictionary<string, string>> interceptTask = InterceptRequestsAsync(redirectUri, responseMode, ct);

            _sessionManager.OpenBrowser(authorizationUri);

            return await interceptTask;
        }

        private async Task<IDictionary<string, string>> InterceptRequestsAsync(
            Uri listenUri, OAuth2ResponseMode responseMode, CancellationToken ct)
        {
            // Create a TaskCompletionSource which completes when we're asked to cancel.
            // We can then await this task together with other tasks that don't take a
            // CancellationToken and exit the method quickly when cancelled.
            var tcs = new TaskCompletionSource<IDictionary<string, string>>();
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
                while (true)
                {
                    Task<HttpListenerContext> contextTask = listener.GetContextAsync();
                    Task<IDictionary<string, string>> cancelTask = tcs.Task;

                    Task completedTask = await Task.WhenAny(contextTask, cancelTask);

                    // Check if we 'completed' the context task or the cancellation task
                    if (completedTask == cancelTask)
                    {
                        // We were cancelled!
                        return await cancelTask;
                    }

                    // We intercepted a request!
                    HttpListenerContext context = await contextTask;

                    IDictionary<string, string> parameters = await GetResponseParametersAsync(context.Request);

                    // In fragment mode the authorization parameters are in the URI fragment, which
                    // user agents do not send to the server. The first leg is therefore a parameterless
                    // GET; reply with a script that reissues the parameters as a form POST so we can
                    // read them from the body on the next iteration.
                    if (responseMode == OAuth2ResponseMode.Fragment && parameters.Count == 0)
                    {
                        await context.Response.WriteResponseAsync(FragmentFormPostHtml);
                        context.Response.Close();
                        continue;
                    }

                    await WriteFinalResponseAsync(context.Response, parameters);

                    return parameters;
                }
            }
            finally
            {
                listener.Stop();
                listener.Close();
            }
        }

        private static async Task<IDictionary<string, string>> GetResponseParametersAsync(HttpListenerRequest request)
        {
            // Form post responses - and the form POST used to forward fragment responses - carry
            // the authorization parameters in the urlencoded request body.
            if (StringComparer.OrdinalIgnoreCase.Equals(request.HttpMethod, Constants.Http.MethodPost) &&
                IsFormUrlEncoded(request.ContentType))
            {
                using var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8);
                string body = await reader.ReadToEndAsync();
                return UriExtensions.ParseQueryString(body);
            }

            // Query responses carry the parameters in the request query string.
            return request.QueryString.ToDictionary(StringComparer.OrdinalIgnoreCase);
        }

        internal static bool IsFormUrlEncoded(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                return false;
            }

            // Compare only the media type, ignoring any parameters such as "; charset=utf-8".
            // The media type is everything up to the first ';'.
            string mediaType = contentType.Split(';')[0].Trim();
            return StringComparer.OrdinalIgnoreCase.Equals(mediaType, Constants.Http.MimeTypeFormUrlEncoded);
        }

        private async Task WriteFinalResponseAsync(HttpListenerResponse response, IDictionary<string, string> parameters)
        {
            // If we have an error value then the request failed and we should reply with a page containing the error information
            bool hasError = parameters.TryGetValue(OAuth2Constants.AuthorizationGrantResponse.ErrorCodeParameter, out string errorCode);
            parameters.TryGetValue(OAuth2Constants.AuthorizationGrantResponse.ErrorDescriptionParameter, out string errorDescription);
            parameters.TryGetValue(OAuth2Constants.AuthorizationGrantResponse.ErrorUriParameter, out string errorUri);
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
