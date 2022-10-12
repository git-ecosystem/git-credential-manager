using System;

namespace GitCredentialManager.Interop.Posix
{
    public class PosixSessionManager : SessionManager
    {
        public PosixSessionManager()
        {
            PlatformUtils.EnsurePosix();
        }

        // Check if we have an X11 or Wayland display environment available
        public override bool IsDesktopSession =>
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DISPLAY")) ||
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"));
    }
}
