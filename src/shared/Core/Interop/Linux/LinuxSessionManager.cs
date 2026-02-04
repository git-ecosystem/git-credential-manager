using System;
using System.Diagnostics;
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

    protected override void OpenBrowserInternal(string url)
    {
        //
        // On Linux, 'shell execute' utilities like xdg-open launch a process without
        // detaching from the standard in/out descriptors. Some applications (like
        // Chromium) write messages to stdout, which is currently hooked up and being
        // consumed by Git, and cause errors.
        //
        // Sadly, the Framework does not allow us to redirect standard streams if we
        // set ProcessStartInfo::UseShellExecute = true, so we must manually launch
        // these utilities and redirect the standard streams manually.
        //
        // We try and use the same 'shell execute' utilities as the Framework does,
        // searching for them in the same order until we find one.
        //
        if (!TryGetShellExecuteHandler(Environment, out string shellExecPath))
        {
            throw new Exception("Failed to locate a utility to launch the default web browser.");
        }

        var psi = new ProcessStartInfo(shellExecPath, url)
        {
            RedirectStandardOutput = true,
            // Ok to redirect stderr for non-git-related processes
            RedirectStandardError = true
        };

        Process.Start(psi);
    }

    private bool GetWebBrowserAvailable()
    {
        // If this is a Windows Subsystem for Linux distribution we may
        // be able to launch the web browser of the host Windows OS.
        if (WslUtils.IsWslDistribution(Environment, FileSystem, out _))
        {
            // We need a shell execute handler to be able to launch to browser
            if (!TryGetShellExecuteHandler(Environment, out _))
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

    private static bool TryGetShellExecuteHandler(IEnvironment env, out string shellExecPath)
    {
        // One additional 'shell execute' utility we also attempt to use over the Framework
        // is `wslview` that is commonly found on WSL (Windows Subsystem for Linux) distributions
        // that opens the browser on the Windows host.
        string[] shellHandlers = { "xdg-open", "gnome-open", "kfmclient", WslUtils.WslViewShellHandlerName };
        foreach (string shellExec in shellHandlers)
        {
            if (env.TryLocateExecutable(shellExec, out shellExecPath))
            {
                return true;
            }
        }

        shellExecPath = null;
        return false;
    }
}
