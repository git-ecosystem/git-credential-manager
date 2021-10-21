using System;

namespace GitCredentialManager.Interop.Posix
{
    public class PosixSessionManager : ISessionManager
    {
        public PosixSessionManager()
        {
            PlatformUtils.EnsurePosix();
        }

        // Check if we have an X11 environment available
        public virtual bool IsDesktopSession => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DISPLAY"));
    }
}
