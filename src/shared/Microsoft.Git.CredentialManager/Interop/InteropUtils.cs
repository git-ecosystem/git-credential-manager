using System;
using System.Runtime.InteropServices;

namespace GitCredentialManager.Interop
{
    internal static class InteropUtils
    {
        public static byte[] ToByteArray(IntPtr ptr, long count)
        {
            var destination = new byte[count];
            Marshal.Copy(ptr, destination, 0, destination.Length);
            return destination;
        }
    }
}
