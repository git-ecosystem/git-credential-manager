using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace GitCredentialManager.Authentication
{
    public interface IWindowsIntegratedAuthentication : IDisposable
    {
        Task<bool> GetIsSupportedAsync(Uri uri);
    }

    public class WindowsIntegratedAuthentication : IWindowsIntegratedAuthentication
    {
        public static readonly string[] AuthorityIds =
        {
            "integrated", "windows", "kerberos", "ntlm",
            "tfs", "sso",
        };

        private readonly ICommandContext _context;

        public WindowsIntegratedAuthentication(ICommandContext context)
        {
            _context = context;
        }

        public async Task<bool> GetIsSupportedAsync(Uri uri)
        {
            EnsureArgument.AbsoluteUri(uri, nameof(uri));

            bool supported = false;

            _context.Trace.WriteLine($"HTTP: HEAD {uri}");
            using (HttpResponseMessage response = await HttpClient.HeadAsync(uri))
            {
                _context.Trace.WriteLine("HTTP: Response code ignored.");

                _context.Trace.WriteLine("Inspecting WWW-Authenticate headers...");
                foreach (AuthenticationHeaderValue wwwHeader in response.Headers.WwwAuthenticate)
                {
                    if (StringComparer.OrdinalIgnoreCase.Equals(wwwHeader.Scheme, Constants.Http.WwwAuthenticateNegotiateScheme))
                    {
                        _context.Trace.WriteLine("Found WWW-Authenticate header for Negotiate");
                        supported = true;
                    }
                    else if (StringComparer.OrdinalIgnoreCase.Equals(wwwHeader.Scheme, Constants.Http.WwwAuthenticateNtlmScheme))
                    {
                        _context.Trace.WriteLine("Found WWW-Authenticate header for NTLM");
                        supported = true;
                    }
                }
            }

            return supported;
        }

        private HttpClient _httpClient;
        private HttpClient HttpClient => _httpClient ?? (_httpClient = _context.HttpClientFactory.CreateClient());

        #region IDisposable

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        #endregion
    }
}
