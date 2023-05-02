using GitCredentialManager.Interop.MacOS.Native;
using GitCredentialManager.Interop.Posix;

namespace GitCredentialManager.Interop.MacOS
{
    public class MacOSSessionManager : PosixSessionManager
    {
        public MacOSSessionManager(IEnvironment env, IFileSystem fs) : base(env, fs)
        {
            PlatformUtils.EnsureMacOS();
        }

        public override bool IsDesktopSession
        {
            get
            {
                // Get information about the current session
                int error = SecurityFramework.SessionGetInfo(SecurityFramework.CallerSecuritySession, out int id, out var sessionFlags);

                // Check if the session supports Quartz
                if (error == 0 && (sessionFlags & SessionAttributeBits.SessionHasGraphicAccess) != 0)
                {
                    return true;
                }

                // Fall-through and check if X11 is available on macOS
                return base.IsDesktopSession;
            }
        }
    }
}
