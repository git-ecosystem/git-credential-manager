using System;
using System.Runtime.InteropServices;

namespace GitCredentialManager.Interop.Posix.Native
{
    public static class Stat
    {
        [DllImport("libc", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int chmod(string path, NativeFileMode mode);
    }
    [Flags]
    public enum NativeFileMode
    {
        NONE = 0,

        // Default permissions (RW for owner, RW for group, RW for other)
        DEFAULT = S_IWOTH | S_IROTH | S_IWGRP | S_IRGRP | S_IWUSR | S_IRUSR,

        // All file access permissions (RWX for owner, group, and other)
        ACCESSPERMS = S_IRWXO | S_IRWXU | S_IRWXG,

        // Read for owner (0000400)
        S_IRUSR = 0x100,
        // Write for owner (0000200)
        S_IWUSR = 0x080,
        // Execute for owner (0000100)
        S_IXUSR = 0x040,
        // Access permissions for owner
        S_IRWXU = S_IRUSR | S_IWUSR | S_IXUSR,

        // Read for group (0000040)
        S_IRGRP = 0x020,
        // Write for group (0000020)
        S_IWGRP = 0x010,
        // Execute for group (0000010)
        S_IXGRP = 0x008,
        // Access permissions for group
        S_IRWXG = S_IRGRP | S_IWGRP | S_IXGRP,

        // Read for other (0000004)
        S_IROTH = 0x004,
        // Write for other (0000002)
        S_IWOTH = 0x002,
        // Execute for other (0000001)
        S_IXOTH = 0x001,
        // Access permissions for other
        S_IRWXO = S_IROTH | S_IWOTH | S_IXOTH,

        // Set user ID on execution (0004000)
        S_ISUID = 0x800,
        // Set group ID on execution (0002000)
        S_ISGID = 0x400,
        // Sticky bit (0001000)
        S_ISVTX = 0x200,
    }
}
