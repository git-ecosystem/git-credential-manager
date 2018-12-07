// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests
{
    public class PlatformFactAttribute : FactAttribute
    {
        public PlatformFactAttribute(params Platform[] platforms)
        {
            if (platforms.Contains(Platform.Windows) && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            if (platforms.Contains(Platform.MacOS) && RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return;
            }

            if (platforms.Contains(Platform.Linux) && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return;
            }

            Skip = "Test not supported on this platform.";
        }
    }

    public enum Platform
    {
        Windows,
        MacOS,
        Linux,
    }
}
