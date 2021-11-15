using System.Runtime.InteropServices;
using GitCredentialManager.Interop.Posix.Native;

namespace GitCredentialManager.Interop.MacOS.Native
{
    public static class Termios_MacOS
    {
        [DllImport("libc", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int tcgetattr(int fd, out termios_MacOS termios);

        [DllImport("libc", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int tcsetattr(int fd, SetActionFlags optActions, ref termios_MacOS termios);
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct termios_MacOS
    {
        // macOS has an array of 20 elements
        private const int NCCS = 20;

        // macOS uses unsigned 64-bit sized flags
        [FieldOffset(0)]  public InputFlags   c_iflag;
        [FieldOffset(8)]  public OutputFlags  c_oflag;
        [FieldOffset(16)] public ControlFlags c_cflag;
        [FieldOffset(24)] public LocalFlags   c_lflag;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NCCS)]
        [FieldOffset(32)] public byte[] c_cc;

        [FieldOffset(32 + NCCS)]     public ulong c_ispeed;
        [FieldOffset(32 + NCCS + 8)] public ulong c_ospeed;
    }
}
