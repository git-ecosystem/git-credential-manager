
namespace GitCredentialManager.Tests.Objects
{
    public class TestSessionManager : ISessionManager
    {
        public bool? IsWebBrowserAvailableOverride { get; set; }

        public bool IsDesktopSession { get; set; }

        bool ISessionManager.IsWebBrowserAvailable => IsWebBrowserAvailableOverride ?? IsDesktopSession;
    }
}
