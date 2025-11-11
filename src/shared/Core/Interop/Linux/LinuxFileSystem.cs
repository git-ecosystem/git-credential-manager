using System;
using System.IO;
using System.Runtime.Versioning;
using GitCredentialManager.Interop.Posix;

namespace GitCredentialManager.Interop.Linux
{
    [SupportedOSPlatform(Constants.LinuxPlatformName)]
    public class LinuxFileSystem : PosixFileSystem
    {
        public override bool IsSamePath(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
            {
                return false;
            }

            // Normalize paths
            a = Path.GetFullPath(a);
            b = Path.GetFullPath(b);

            // Resolve symbolic links
            a = ResolveSymbolicLinks(a);
            b = ResolveSymbolicLinks(b);

            return StringComparer.Ordinal.Equals(a, b);
        }
    }
}
