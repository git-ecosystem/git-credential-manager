// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Runtime.InteropServices;

namespace Microsoft.Git.CredentialManager
{
    public static class PlatformUtils
    {
        /// <summary>
        /// Get information about the current platform (OS and CLR details).
        /// </summary>
        /// <returns>Platform information.</returns>
        public static PlatformInformation GetPlatformInformation()
        {
            string osType = GetOSType();
            string cpuArch = GetCpuArchitecture();
            string clrVersion = GetClrVersion();

            return new PlatformInformation(osType, cpuArch, clrVersion);
        }

        /// <summary>
        /// Check if the current Operating System is macOS.
        /// </summary>
        /// <returns>True if running on macOS, false otherwise.</returns>
        public static bool IsMacOS()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        }

        /// <summary>
        /// Check if the current Operating System is Windows.
        /// </summary>
        /// <returns>True if running on Windows, false otherwise.</returns>
        public static bool IsWindows()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }

        /// <summary>
        /// Check if the current Operating System is Linux-based.
        /// </summary>
        /// <returns>True if running on a Linux distribution, false otherwise.</returns>
        public static bool IsLinux()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        }

        /// <summary>
        /// Ensure the current Operating System is macOS, fail otherwise.
        /// </summary>
        /// <exception cref="PlatformNotSupportedException">Thrown if the current OS is not macOS.</exception>
        public static void EnsureMacOS()
        {
            if (!IsMacOS())
            {
                throw new PlatformNotSupportedException();
            }
        }

        /// <summary>
        /// Ensure the current Operating System is Windows, fail otherwise.
        /// </summary>
        /// <exception cref="PlatformNotSupportedException">Thrown if the current OS is not Windows.</exception>
        public static void EnsureWindows()
        {
            if (!IsWindows())
            {
                throw new PlatformNotSupportedException();
            }
        }

        /// <summary>
        /// Ensure the current Operating System is Linux-based, fail otherwise.
        /// </summary>
        /// <exception cref="PlatformNotSupportedException">Thrown if the current OS is not Linux-based.</exception>
        public static void EnsureLinux()
        {
            if (!IsLinux())
            {
                throw new PlatformNotSupportedException();
            }
        }

        #region Platform information helper methods

        private static string GetOSType()
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

        private static string GetCpuArchitecture()
        {
            switch (RuntimeInformation.OSArchitecture)
            {
                case Architecture.Arm:
                    return "ARM32";
                case Architecture.Arm64:
                    return "ARM64";
                case Architecture.X64:
                    return "x86-64";
                case Architecture.X86:
                    return "x86";
                default:
                    return RuntimeInformation.OSArchitecture.ToString();
            }
        }

        private static string GetClrVersion()
        {
            return RuntimeInformation.FrameworkDescription;
        }

        #endregion
    }

    public struct PlatformInformation
    {
        public PlatformInformation(string osType, string cpuArch, string clrVersion)
        {
            OperatingSystemType = osType;
            CpuArchitecture = cpuArch;
            ClrVersion = clrVersion;
        }

        public readonly string OperatingSystemType;
        public readonly string CpuArchitecture;
        public readonly string ClrVersion;
    }
}
