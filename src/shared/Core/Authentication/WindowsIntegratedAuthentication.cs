using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace GitCredentialManager.Authentication
{
    public interface IWindowsIntegratedAuthentication : IDisposable
    {
        Task<WindowsAuthenticationTypes> GetAuthenticationTypesAsync(Uri uri);
    }

    [Flags]
    public enum WindowsAuthenticationTypes
    {
        None,
        Ntlm,
        Negotiate,

        All = Ntlm | Negotiate
    }

    public class WindowsIntegratedAuthentication : AuthenticationBase, IWindowsIntegratedAuthentication
    {
        public static readonly string[] AuthorityIds =
        {
            "integrated", "windows", "kerberos", "ntlm",
            "tfs", "sso",
        };

        public WindowsIntegratedAuthentication(ICommandContext context)
            : base(context) { }

        public async Task<WindowsAuthenticationTypes> GetAuthenticationTypesAsync(Uri uri)
        {
            EnsureArgument.AbsoluteUri(uri, nameof(uri));

            var types = WindowsAuthenticationTypes.None;

            Context.Trace.WriteLine($"HTTP: HEAD {uri}");
            using (HttpResponseMessage response = await HttpClient.HeadAsync(uri))
            {
                Context.Trace.WriteLine("HTTP: Response code ignored.");

                Context.Trace.WriteLine("Inspecting WWW-Authenticate headers...");
                foreach (AuthenticationHeaderValue wwwHeader in response.Headers.WwwAuthenticate)
                {
                    if (StringComparer.OrdinalIgnoreCase.Equals(wwwHeader.Scheme, Constants.Http.WwwAuthenticateNegotiateScheme))
                    {
                        Context.Trace.WriteLine("Found WWW-Authenticate header for Negotiate");
                        types |= WindowsAuthenticationTypes.Negotiate;
                    }
                    else if (StringComparer.OrdinalIgnoreCase.Equals(wwwHeader.Scheme, Constants.Http.WwwAuthenticateNtlmScheme))
                    {
                        Context.Trace.WriteLine("Found WWW-Authenticate header for NTLM");
                        types |= WindowsAuthenticationTypes.Ntlm;
                    }
                }
            }

            return types;
        }

        private HttpClient _httpClient;
        private HttpClient HttpClient => _httpClient ?? (_httpClient = Context.HttpClientFactory.CreateClient());

        #region IDisposable

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        #endregion
    }
}
