using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Git.CredentialManager
{
    public static class PlatformUtils
    {
        public static string GetOSInfo()
        {
            if (IsWindows())
            {
                return "Windows";
            }

            if (IsMacOS())
            {
                return "macOS";
            }

            if (IsLinux())
            {
                return "Linux";
            }

            return "Unknown";
        }

        public static bool IsMacOS()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        }

        public static bool IsWindows()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }

        public static bool IsLinux()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        }

        public static void EnsureMacOS()
        {
            if (!IsMacOS())
            {
                throw new PlatformNotSupportedException();
            }
        }

        public static void EnsureWindows()
        {
            if (!IsWindows())
            {
                throw new PlatformNotSupportedException();
            }
        }

        public static void EnsureLinux()
        {
            if (!IsLinux())
            {
                throw new PlatformNotSupportedException();
            }
        }
    }
}
