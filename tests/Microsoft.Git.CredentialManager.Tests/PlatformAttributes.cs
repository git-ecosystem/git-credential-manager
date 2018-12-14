// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests
{
    public class PlatformFactAttribute : FactAttribute
    {
        public PlatformFactAttribute(params Platform[] platforms)
        {
            if (!XunitHelpers.IsSupportedPlatform(platforms))
            {
                Skip = "Test not supported on this platform.";
            }
        }
    }

    public class PlatformTheoryAttribute : TheoryAttribute
    {
        public PlatformTheoryAttribute(params Platform[] platforms)
        {
            if (!XunitHelpers.IsSupportedPlatform(platforms))
            {
                Skip = "Test not supported on this platform.";
            }
        }
    }

    internal static class XunitHelpers
    {
        public static bool IsSupportedPlatform(Platform[] platforms)
        {
            if (platforms.Contains(Platform.Windows) && RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
                platforms.Contains(Platform.MacOS)   && RuntimeInformation.IsOSPlatform(OSPlatform.OSX)     ||
                platforms.Contains(Platform.Linux)   && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return true;
            }

            return false;
        }
    }

    public enum Platform
    {
        Windows,
        MacOS,
        Linux,
    }
}
