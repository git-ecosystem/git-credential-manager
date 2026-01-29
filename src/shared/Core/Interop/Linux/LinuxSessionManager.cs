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
        // Determine system properties
        bool hasHandler = BrowserUtils.TryGetLinuxShellExecuteHandler(Environment, out _);
        bool isWsl = WslUtils.IsWslDistribution(Environment, FileSystem, out _);
        bool isWslSession0 = isWsl && WslUtils.GetWindowsSessionId(FileSystem) == 0;
        bool isDesktopSession = IsDesktopSession;

        //
        // WSL session 0 note:
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

        // We can launch a browser if:
        // 1. We have a shell execute handler (xdg-open, wslview, etc.) and not blocked by WSL session 0, OR
        // 2. We have a desktop session on non-WSL Linux (backward compatibility - assumes handler exists)
        return (hasHandler && !isWslSession0) || (isDesktopSession && !isWsl && !hasHandler);
    }
}
