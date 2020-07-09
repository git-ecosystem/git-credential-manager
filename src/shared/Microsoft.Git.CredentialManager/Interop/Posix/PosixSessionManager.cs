// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;

namespace Microsoft.Git.CredentialManager.Interop.Posix
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
