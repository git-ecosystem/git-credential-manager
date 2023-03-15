using System;
using System.Diagnostics;

namespace GitCredentialManager
{
    public static class BrowserUtils
    {
        public static void OpenDefaultBrowser(IEnvironment environment, string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            {
                throw new ArgumentException($"Not a valid URI: '{url}'");
            }

            OpenDefaultBrowser(environment, uri);
        }

        public static void OpenDefaultBrowser(IEnvironment environment, Uri uri)
        {
            if (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Can only open HTTP/HTTPS URIs", nameof(uri));
            }

            string url = uri.ToString();

            ProcessStartInfo psi;
            if (PlatformUtils.IsLinux())
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
                if (!TryGetLinuxShellExecuteHandler(environment, out string shellExecPath))
                {
                    throw new Exception("Failed to locate a utility to launch the default web browser.");
                }

                psi = new ProcessStartInfo(shellExecPath, url)
                {
                    RedirectStandardOutput = true,
                    // Ok to redirect stderr for non-git-related processes
                    RedirectStandardError = true
                };
            }
            else
            {
                // On Windows and macOS, `ShellExecute` and `/usr/bin/open` disconnect the child process
                // from our standard in/out streams, so we can just use the Framework to do this.
                psi = new ProcessStartInfo(url) {UseShellExecute = true};
            }

            // We purposefully do not use a ChildProcess here, as the purpose of that
            // class is to allow us to collect child process information using TRACE2.
            // Since we will not be collecting TRACE2 data from the browser, there
            // is no need to add the extra overhead associated with ChildProcess here.
            Process.Start(psi);
        }

        public static bool TryGetLinuxShellExecuteHandler(IEnvironment env, out string shellExecPath)
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
}
