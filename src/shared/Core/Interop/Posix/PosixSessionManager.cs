namespace GitCredentialManager.Interop.Posix
{
    public abstract class PosixSessionManager : SessionManager
    {
        protected PosixSessionManager(IEnvironment env, IFileSystem fs) : base(env, fs)
        {
            PlatformUtils.EnsurePosix();
        }

        // Check if we have an X11 or Wayland display environment available
        public override bool IsDesktopSession =>
            !string.IsNullOrWhiteSpace(System.Environment.GetEnvironmentVariable("DISPLAY")) ||
            !string.IsNullOrWhiteSpace(System.Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"));
    }
}
