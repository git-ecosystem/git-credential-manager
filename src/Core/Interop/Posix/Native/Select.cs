using System.Runtime.InteropServices;

namespace GitCredentialManager.Interop.Posix.Native;

public static class Select
{
    // fd_set is 1024 bits on both macOS and Linux, i.e. 128 bytes. /dev/tty
    // descriptors are always small, so a fixed-size byte buffer with manual
    // bit-setting is portable across both platforms' fd_set word sizes
    // (little-endian bit ordering is identical for 32- and 64-bit words).
    private const int FdSetSizeBytes = 128;

    [StructLayout(LayoutKind.Sequential)]
    public struct Timeval
    {
        public long tv_sec;

        // tv_usec is a 32-bit field on macOS and 64-bit on Linux. Declaring it
        // as a 64-bit field is correct on both as long as the value fits in
        // 32 bits: on macOS the kernel reads only the low 4 bytes and the high
        // 4 bytes are zero padding (little-endian).
        public long tv_usec;
    }

    [DllImport("libc", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    private static extern int select(int nfds, byte[] readfds, byte[] writefds, byte[] exceptfds, ref Timeval timeout);

    /// <summary>
    /// Wait until <paramref name="fd"/> is readable or <paramref name="timeoutMs"/>
    /// elapses. A timeout of <c>0</c> polls without blocking.
    /// </summary>
    /// <returns>True if the descriptor is readable; false on timeout or error.</returns>
    public static bool WaitReadable(int fd, int timeoutMs)
    {
        byte[] readfds = new byte[FdSetSizeBytes];
        readfds[fd / 8] |= (byte)(1 << (fd % 8));

        var timeout = new Timeval
        {
            tv_sec = timeoutMs / 1000,
            tv_usec = (timeoutMs % 1000) * 1000,
        };

        int result = select(fd + 1, readfds, null, null, ref timeout);
        if (result <= 0)
        {
            return false;
        }

        return (readfds[fd / 8] & (1 << (fd % 8))) != 0;
    }
}
