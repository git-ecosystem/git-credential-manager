using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace GitCredentialManager
{
    public static class WslUtils
    {
        private const string WslUncPrefix = @"\\wsl$\";
        private const string WslLocalHostUncPrefix = @"\\wsl.localhost\";
        private const string WslCommandName = "wsl.exe";
        private const string WslInteropEnvar = "WSL_INTEROP";

        /// <summary>
        /// Cached WSL version.
        /// </summary>
        /// <remarks>A value of 0 represents "not WSL", and a value less than 0 represents "unknown".</remarks>
        private static int _wslVersion = -1;

        public static bool IsWslDistribution(IEnvironment env, IFileSystem fs, out int wslVersion)
        {
            if (_wslVersion < 0)
            {
                _wslVersion = GetWslVersion(env, fs);
            }

            wslVersion = _wslVersion;
            return _wslVersion > 0;
        }

        private static int GetWslVersion(IEnvironment env, IFileSystem fs)
        {
            // All WSL distributions are Linux.. obviously!
            if (!PlatformUtils.IsLinux())
            {
                return 0;
            }

            // The WSL_INTEROP variable is set in WSL2 distributions
            if (env.Variables.TryGetValue(WslInteropEnvar, out _))
            {
                return 2;
            }

            const string procVersionPath = "/proc/version";
            if (fs.FileExists(procVersionPath))
            {
                // Both WSL1 and WSL2 distributions include "[Mm]icrosoft" in the version string
                string procVersion = fs.ReadAllText(procVersionPath);
                if (!Regex.IsMatch(procVersion, "[Mm]icrosoft"))
                {
                    return 0;
                }

                // WSL2 distributions return "WSL2" in the version string
                if (Regex.IsMatch(procVersion, "wsl2", RegexOptions.IgnoreCase))
                {
                    return 2;
                }

                return 1;
            }

            return 0;
        }

        /// <summary>
        /// Test if a file path points to a location in a Windows Subsystem for Linux distribution.
        /// </summary>
        /// <param name="path">Path to test.</param>
        /// <returns>True if <paramref name="path"/> is a WSL path, false otherwise.</returns>
        public static bool IsWslPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;

            return (path.StartsWith(WslUncPrefix, StringComparison.OrdinalIgnoreCase) &&
                    path.Length > WslUncPrefix.Length) ||
                   (path.StartsWith(WslLocalHostUncPrefix, StringComparison.OrdinalIgnoreCase) &&
                    path.Length > WslLocalHostUncPrefix.Length);
        }

        /// <summary>
        /// Create a command to be executed in a Windows Subsystem for Linux distribution.
        /// </summary>
        /// <param name="distribution">WSL distribution name.</param>
        /// <param name="command">Command to execute.</param>
        /// <param name="trace2">The applications TRACE2 tracer.</param>
        /// <param name="workingDirectory">Optional working directory.</param>
        /// <returns><see cref="Process"/> object ready to start.</returns>
        public static ChildProcess CreateWslProcess(string distribution,
            string command,
            ITrace2 trace2,
            string workingDirectory = null)
        {
            var args = new StringBuilder();
            args.AppendFormat("--distribution {0} ", distribution);
            args.AppendFormat("--exec {0}", command);

            string wslExePath = GetWslPath();

            var psi = new ProcessStartInfo(wslExePath, args.ToString())
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = false, // Do not redirect stderr as tracing might be enabled
                UseShellExecute = false,
                WorkingDirectory = workingDirectory ?? string.Empty
            };

            return new ChildProcess(trace2, psi);
        }

        public static string ConvertToDistroPath(string path, out string distribution)
        {
            if (!IsWslPath(path)) throw new ArgumentException("Must provide a WSL path", nameof(path));

            int distroStart;
            if (path.StartsWith(WslUncPrefix, StringComparison.OrdinalIgnoreCase))
            {
                distroStart = WslUncPrefix.Length;
            }
            else if (path.StartsWith(WslLocalHostUncPrefix, StringComparison.OrdinalIgnoreCase))
            {
                distroStart = WslLocalHostUncPrefix.Length;
            }
            else
            {
                throw new Exception("Invalid WSL path prefix");
            }

            int distroEnd = path.IndexOf('\\', distroStart);

            if (distroEnd < 0) distroEnd = path.Length;

            distribution = path.Substring(distroStart, distroEnd - distroStart);

            if (path.Length > distroEnd)
            {
                return path.Substring(distroEnd).Replace('\\', '/');
            }

            return "/";
        }

        internal /*for testing purposes*/ static string GetWslPath()
        {
            // WSL is only supported on 64-bit operating systems
            if (!Environment.Is64BitOperatingSystem)
            {
                throw new Exception("WSL is not supported on 32-bit operating systems");
            }

            //
            // When running as a 32-bit application on a 64-bit operating system, we cannot access the real
            // C:\Windows\System32 directory because the OS will redirect us transparently to the
            // C:\Windows\SysWOW64 directory (containing 32-bit executables).
            //
            // In order to access the real 64-bit System32 directory, we must access via the pseudo directory
            // C:\Windows\SysNative that does **not** experience any redirection for 32-bit applications.
            //
            // HOWEVER, if we're running as a 64-bit application on a 64-bit operating system, the SysNative
            // directory does not exist! This means if running as a 64-bit application on a 64-bit OS we must
            // use the System32 directory name directly.
            //
            var sysDir = Environment.ExpandEnvironmentVariables(
                Environment.Is64BitProcess
                    ? @"%WINDIR%\System32"
                    : @"%WINDIR%\SysNative"
            );

            return Path.Combine(sysDir, WslCommandName);
        }
    }
}
