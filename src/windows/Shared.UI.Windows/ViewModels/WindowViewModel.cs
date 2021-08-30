using System;
using System.Diagnostics;

namespace Microsoft.Git.CredentialManager.UI.ViewModels
{
    public abstract class WindowViewModel : ViewModel
    {
        public abstract string Title { get; }

        public event EventHandler Accepted;
        public event EventHandler Canceled;

        public void Accept()
        {
            Accepted?.Invoke(this, EventArgs.Empty);
        }

        public void Cancel()
        {
            Canceled?.Invoke(this, EventArgs.Empty);
        }

        public static void OpenDefaultBrowser(string url)
        {
            if (!url.StartsWith(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Can only open HTTP/HTTPS URLs", nameof(url));
            }

            var psi = new ProcessStartInfo(url)
            {
                UseShellExecute = true
            };

            Process.Start(psi);
        }
    }
}
