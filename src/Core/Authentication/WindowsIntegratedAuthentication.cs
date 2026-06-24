using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager.Tty;
using GitCredentialManager.UI;
using GitCredentialManager.UI.ViewModels;
using GitCredentialManager.UI.Views;
using Spectre.Console;

namespace GitCredentialManager.Authentication
{
    public interface IWindowsIntegratedAuthentication : IDisposable
    {
        Task<NtlmSupport> AskEnableNtlmAsync(Uri uri);
        Task<WindowsAuthenticationTypes> GetAuthenticationTypesAsync(Uri uri);
    }

    public enum NtlmSupport
    {
        Once,
        Always,
        Disabled,
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

        public async Task<NtlmSupport> AskEnableNtlmAsync(Uri uri)
        {
            ThrowIfUserInteractionDisabled();

            if (Context.SessionManager.IsDesktopSession && Context.Settings.IsGuiPromptsEnabled)
            {
                // Note: we do not support the UI helper for WIA so always show the in-proc GUI
                var vm = new EnableNtlmViewModel(Context.SessionManager)
                {
                    Url = uri.ToString(),
                };
                await AvaloniaUi.ShowViewAsync<EnableNtlmView>(vm, GetParentWindowHandle(), CancellationToken.None);
                ThrowIfWindowCancelled(vm);

                return vm.SelectedOption;
            }

            ThrowIfTerminalPromptsDisabled();

            var prompt = TerminalPrompts.CreateSelection<int>()
                .Title("Re-enable NTLM support in Git?");

            prompt.AddChoice("Yes - just this time", 0);
            prompt.AddChoice($"Yes - always for {uri}", 1);
            prompt.AddChoice("No - do not enable NTLM", 2);

            int choice = await prompt.ShowAsync(Context.Console);
            return choice switch
            {
                0 => NtlmSupport.Once,
                1 => NtlmSupport.Always,
                _ => NtlmSupport.Disabled
            };
        }

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
