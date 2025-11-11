using System;
using System.IO;
using System.Runtime.Versioning;

namespace GitCredentialManager.Interop.Windows
{
    [SupportedOSPlatform(Constants.WindowsPlatformName)]
    public class WindowsFileSystem : FileSystem
    {
        public override bool IsSamePath(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
            {
                return false;
            }

            a = Path.GetFullPath(a);
            b = Path.GetFullPath(b);

            // Note: we do not resolve or handle symlinks on Windows
            // because they require administrator permissions to even create!

            return StringComparer.OrdinalIgnoreCase.Equals(a, b);
        }
    }
}
