using GitCredentialManager.Interop.Posix;

namespace GitCredentialManager.Interop.Linux;

public class LinuxSessionManager : PosixSessionManager
{
    private bool? _isWebBrowserAvailable;

    public LinuxSessionManager(IEnvironment env, IFileSystem fs) : base(env, fs)
    {
        PlatformUtils.EnsureLinux();
    }
    
    public override bool IsWebBrowserAvailable
    {
        get
        {
            return _isWebBrowserAvailable ??= GetWebBrowserAvailable();
        }
    }
    
    private bool GetWebBrowserAvailable()
    {
        // If this is a Windows Subsystem for Linux distribution we may
        // be able to launch the web browser of the host Windows OS.
        if (WslUtils.IsWslDistribution(Environment, FileSystem, out _))
        {
            // We need a shell execute handler to be able to launch to browser
            if (!BrowserUtils.TryGetLinuxShellExecuteHandler(Environment, out _))
            {
                return false;
            }

            //
            // If we are in Windows logon session 0 then the user can never interact,
            // even in the WinSta0 window station. This is typical when SSH-ing into a
            // Windows 10+ machine using the default OpenSSH Server configuration,
            // which runs in the 'services' session 0.
            //
            // If we're in any other session, and in the WinSta0 window station then
            // the user can possibly interact. However, since it's hard to determine
            // the window station from PowerShell cmdlets (we'd need to write P/Invoke
            // code and that's just messy and too many levels of indirection quite
            // frankly!) we just assume any non session 0 is interactive.
            //
            // This assumption doesn't hold true if the user has changed the user that
            // the OpenSSH Server service runs as (not a built-in NT service) *AND*
            // they've SSH-ed into the Windows host (and then started a WSL shell).
            // This feels like a very small subset of users...
            //
            if (WslUtils.GetWindowsSessionId(FileSystem) == 0)
            {
                return false;
            }

            // If we are not in session 0, or we cannot get the Windows session ID,
            // assume that we *CAN* launch the browser so that users are never blocked.
            return true;
        }

        // We require an interactive desktop session to be able to launch a browser
        return IsDesktopSession;
    }
}
