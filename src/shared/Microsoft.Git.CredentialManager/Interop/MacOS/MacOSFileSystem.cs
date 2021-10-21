using System;
using System.IO;
using GitCredentialManager.Interop.Posix;

namespace GitCredentialManager.Interop.MacOS
{
    public class MacOSFileSystem : PosixFileSystem
    {
        public override bool IsSamePath(string a, string b)
        {
            a = Path.GetFileName(a);
            b = Path.GetFileName(b);

            // TODO: resolve symlinks
            // TODO: check if APFS/HFS+ is in case-sensitive mode
            return StringComparer.OrdinalIgnoreCase.Equals(a, b);
        }
    }
}
