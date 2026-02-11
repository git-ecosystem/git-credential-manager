using System;
using System.Diagnostics;
using GitCredentialManager.Interop.Posix;

namespace GitCredentialManager.Interop.Linux;

public class LinuxSessionManager : PosixSessionManager
{
    private bool? _isWebBrowserAvailable;

    public LinuxSessionManager(ITrace trace, IEnvironment env, IFileSystem fs) : base(trace, env, fs)
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

        Trace.WriteLine($"Opening browser using '{shellExecPath}: {url}");

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
        // We need a shell execute handler to be able to launch to browser
        if (!TryGetShellExecuteHandler(Environment, out _))
        {
            Trace.WriteLine("Could not locate a shell execute handler for Linux - browser is not available.");
            return false;
        }

        // If this is a Windows Subsystem for Linux distribution we may
        // be able to launch the web browser of the host Windows OS, but
        // there are further checks to do on the Windows host's session.
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
        if (WslUtils.IsWslDistribution(Environment, FileSystem, out _))
        {
            if (WslUtils.GetWindowsSessionId(FileSystem) == 0)
            {
                Trace.WriteLine("This is a WSL distribution, but Windows session 0 was detected - browser is not available.");
                return false;
            }

            // Not on session 0 - we assume the user can interact with browser on Windows.
            Trace.WriteLine("This is a WSL distribution - browser is available.");
            return true;
        }

        //
        // We may also be able to launch a browser if we're inside a Visual Studio Code remote session.
        // VSCode overrides the BROWSER environment variable to a script that allows the user to open
        // the browser on their client machine.
        //
        // Even though we can start a browser, one piece of critical functionality we need is the ability
        // to have that browser be able to connect back to GCM over localhost. There are several types
        // of VSCode remote session, and only some of them automatically forward ports in such a way that
        // the client browser can automatically connect back to GCM over localhost.
        //
        // * SSH [OK]
        //   Connection over SSH to a remote machine.
        //
        // * Dev Containers [OK]
        //   Connection to a container.
        //
        // * Dev Tunnels [Not OK - forwarded ports not accessible on the client via localhost]
        //   Connection to a remote machine over the Internet using Microsoft Dev Tunnels.
        //
        // * WSL [Ignored - already handled above]
        //
        if (Environment.Variables.ContainsKey("VSCODE_IPC_HOOK_CLI") &&
            Environment.Variables.ContainsKey("BROWSER"))
        {
            // Looking for SSH_CONNECTION tells us we're connected via SSH.
            // HOWEVER, we may also see SSH_CONNECTION in a Dev Tunnel session if the tunnel server
            // process was started within an SSH session (and the SSH_CONNECTION environment variable
            // was inherited).
            // We therefore check for the absence of the SSH_TTY variable, which gets unset
            // in Dev Tunnel sessions but is always still set in regular SSH sessions.
            if (Environment.Variables.ContainsKey("SSH_CONNECTION") &&
                !Environment.Variables.ContainsKey("SSH_TTY"))
            {
                Trace.WriteLine("VSCode (Remote SSH) detected - browser is available.");
                return true;
            }

            if (Environment.Variables.ContainsKey("REMOTE_CONTAINERS"))
            {
                Trace.WriteLine("VSCode (Dev Containers) detected - browser is available.");
                return true;
            }

            Trace.WriteLine("VSCode (Remote Tunnel) detected - browser is not available.");
            return false;
        }

        // We need a desktop session to be able to launch the browser in the general case
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
