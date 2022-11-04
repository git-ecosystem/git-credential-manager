using System;
using System.IO;
using GitCredentialManager.Interop.Posix;

namespace GitCredentialManager.Interop.MacOS
{
    public class MacOSFileSystem : PosixFileSystem
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

            // TODO: determine if file system is case-sensitive
            // By default HFS+/APFS is NOT case-sensitive...
            return StringComparer.OrdinalIgnoreCase.Equals(a, b);
        }
    }
}
