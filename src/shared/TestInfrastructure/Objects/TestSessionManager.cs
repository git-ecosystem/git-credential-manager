using System;

namespace GitCredentialManager.Tests.Objects
{
    public class TestSessionManager : ISessionManager
    {
        public bool? IsWebBrowserAvailableOverride { get; set; }

        public bool IsDesktopSession { get; set; }

        public Action<Uri> OpenBrowserFunc { get; set; } = _ => { };

        bool ISessionManager.IsWebBrowserAvailable => IsWebBrowserAvailableOverride ?? IsDesktopSession;

        void ISessionManager.OpenBrowser(Uri uri) => OpenBrowserFunc(uri);
    }
}
