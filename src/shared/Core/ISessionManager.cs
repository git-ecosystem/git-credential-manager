using System;
using System.Diagnostics;

namespace GitCredentialManager
{
    public interface ISessionManager
    {
        /// <summary>
        /// Determine if the current session has access to a desktop/can display UI.
        /// </summary>
        /// <returns>True if the session can display UI, false otherwise.</returns>
        bool IsDesktopSession { get; }

        /// <summary>
        /// Determine if the current session has access to a web browser.
        /// </summary>
        /// <returns>True if the session can display a web browser, false otherwise.</returns>
        bool IsWebBrowserAvailable { get; }

        /// <summary>
        /// Open the system web browser to the specified URL.
        /// </summary>
        /// <param name="uri"><see cref="Uri"/> to open the browser at.</param>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="IsWebBrowserAvailable"/> is false.</exception>
        void OpenBrowser(Uri uri);
    }

    public static class SessionManagerExtensions
    {
        public static void OpenBrowser(this ISessionManager sm, string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            {
                throw new ArgumentException($"Not a valid URI: '{url}'");
            }

            sm.OpenBrowser(uri);
        }
    }
    
    public abstract class SessionManager : ISessionManager
    {
        protected ITrace Trace { get; }
        protected IEnvironment Environment { get; }
        protected IFileSystem FileSystem { get; }

        protected SessionManager(ITrace trace, IEnvironment env, IFileSystem fs)
        {
            EnsureArgument.NotNull(trace, nameof(trace));
            EnsureArgument.NotNull(env, nameof(env));
            EnsureArgument.NotNull(fs, nameof(fs));

            Trace = trace;
            Environment = env;
            FileSystem = fs;
        }
        
        public abstract bool IsDesktopSession { get; }

        public virtual bool IsWebBrowserAvailable => IsDesktopSession;

        public void OpenBrowser(Uri uri)
        {
            if (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Can only open HTTP/HTTPS URIs", nameof(uri));
            }

            OpenBrowserInternal(uri.ToString());
        }

        protected virtual void OpenBrowserInternal(string url)
        {
            Trace.WriteLine("Opening browser using framework shell-execute: " + url);
            var psi = new ProcessStartInfo(url) { UseShellExecute = true };
            Process.Start(psi);
        }
    }
}
