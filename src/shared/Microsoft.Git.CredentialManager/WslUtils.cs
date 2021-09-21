using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Microsoft.Git.CredentialManager
{
    public static class WslUtils
    {
        private const string WslUncPrefix = @"\\wsl$\";
        private const string WslCommandName = "wsl.exe";

        /// <summary>
        /// Test if a file path points to a location in a Windows Subsystem for Linux distribution.
        /// </summary>
        /// <param name="path">Path to test.</param>
        /// <returns>True if <paramref name="path"/> is a WSL path, false otherwise.</returns>
        public static bool IsWslPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;

            return path.StartsWith(WslUncPrefix, StringComparison.OrdinalIgnoreCase) &&
                   path.Length > WslUncPrefix.Length;
        }

        /// <summary>
        /// Create a command to be executed in a Windows Subsystem for Linux distribution.
        /// </summary>
        /// <param name="distribution">WSL distribution name.</param>
        /// <param name="command">Command to execute.</param>
        /// <param name="workingDirectory">Optional working directory.</param>
        /// <returns><see cref="Process"/> object ready to start.</returns>
        public static Process CreateWslProcess(string distribution, string command, string workingDirectory = null)
        {
            var args = new StringBuilder();
            args.AppendFormat("--distribution {0} ", distribution);
            args.AppendFormat("--exec {0}", command);

            string wslExePath = GetWslPath();

            var psi = new ProcessStartInfo(wslExePath, args.ToString())
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = workingDirectory ?? string.Empty
            };

            return new Process { StartInfo = psi };
        }

        public static string ConvertToDistroPath(string path, out string distribution)
        {
            if (!IsWslPath(path)) throw new ArgumentException("Must provide a WSL path", nameof(path));

            int distroStart = WslUncPrefix.Length;
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
            // directory does not exist! This means if running as a 32-bit application on a 64-bit OS we must
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
