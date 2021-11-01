using System;
using System.Runtime.InteropServices;
using Xunit;

namespace GitCredentialManager.Tests
{
    public class PlatformFactAttribute : FactAttribute
    {
        public PlatformFactAttribute(Platforms platforms)
        {
            if (!XunitHelpers.IsSupportedPlatform(platforms))
            {
                Skip = "Test not supported on this platform.";
            }
        }
    }

    public class PlatformTheoryAttribute : TheoryAttribute
    {
        public PlatformTheoryAttribute(Platforms platforms)
        {
            if (!XunitHelpers.IsSupportedPlatform(platforms))
            {
                Skip = "Test not supported on this platform.";
            }
        }
    }

    public class SkippablePlatformFactAttribute : SkippableFactAttribute
    {
        public SkippablePlatformFactAttribute(Platforms platforms)
        {
            Xunit.Skip.IfNot(
                XunitHelpers.IsSupportedPlatform(platforms),
                "Test not supported on this platform."
            );
        }
    }

    public class SkippablePlatformTheoryAttribute : SkippableTheoryAttribute
    {
        public SkippablePlatformTheoryAttribute(Platforms platforms)
        {
            Xunit.Skip.IfNot(
                XunitHelpers.IsSupportedPlatform(platforms),
                "Test not supported on this platform."
            );
        }
    }

    internal static class XunitHelpers
    {
        public static bool IsSupportedPlatform(Platforms platforms)
        {
            if (platforms.HasFlag(Platforms.Windows) && RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
                platforms.HasFlag(Platforms.MacOS)   && RuntimeInformation.IsOSPlatform(OSPlatform.OSX)     ||
                platforms.HasFlag(Platforms.Linux)   && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return true;
            }

            return false;
        }
    }

    [Flags]
    public enum Platforms
    {
        None    = 0,
        Windows = 1 << 0,
        MacOS   = 1 << 2,
        Linux   = 1 << 3,
        Posix   = MacOS | Linux,
        All     = Windows | Posix
    }
}
