// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Microsoft.Git.CredentialManager.Interop.MacOS.Native;
using Microsoft.Git.CredentialManager.Interop.Posix;

namespace Microsoft.Git.CredentialManager.Interop.MacOS
{
    public class MacOSSessionManager : PosixSessionManager
    {
        public MacOSSessionManager()
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
