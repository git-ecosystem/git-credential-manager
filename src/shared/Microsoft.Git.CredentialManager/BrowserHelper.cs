using System;
using System.Diagnostics;

namespace Microsoft.Git.CredentialManager
{
    public static class BrowserHelper
    {
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

        public static void OpenDefaultBrowser(Uri uri) => OpenDefaultBrowser(uri.ToString());
    }
}
