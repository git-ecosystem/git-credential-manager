using System.Runtime.InteropServices;
using GitCredentialManager.Interop.Posix.Native;

namespace GitCredentialManager.Interop.Linux.Native
{
    public static class Termios_Linux
    {
        [DllImport("libc", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int tcgetattr(int fd, out termios_Linux termios);

        [DllImport("libc", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int tcsetattr(int fd, SetActionFlags optActions, ref termios_Linux termios);
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct termios_Linux
    {
        // Linux has an array of 32 elements
        private const int NCCS = 32;

        // Linux uses unsigned 32-bit sized flags
        [FieldOffset(0)]  public InputFlags   c_iflag;
        [FieldOffset(4)]  public OutputFlags  c_oflag;
        [FieldOffset(8)]  public ControlFlags c_cflag;
        [FieldOffset(12)] public LocalFlags   c_lflag;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NCCS)]
        [FieldOffset(16)] public byte[] c_cc;

        [FieldOffset(16 + NCCS)]     public uint c_ispeed;
        [FieldOffset(16 + NCCS + 4)] public uint c_ospeed;
    }
}
