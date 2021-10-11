using System;
using System.IO;
using Microsoft.Git.CredentialManager.Interop.Posix;

namespace Microsoft.Git.CredentialManager.Interop.Linux
{
    public class LinuxFileSystem : PosixFileSystem
    {
        public override bool IsSamePath(string a, string b)
        {
            a = Path.GetFileName(a);
            b = Path.GetFileName(b);

            return StringComparer.Ordinal.Equals(a, b);
        }
    }
}
