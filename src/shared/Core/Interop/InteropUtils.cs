using System;
using System.Linq;
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

        public static bool AreEqual(byte[] bytes, IntPtr ptr, uint length)
        {
            if (bytes.Length == 0 && (ptr == IntPtr.Zero || length == 0))
            {
                return true;
            }

            if (bytes.Length != length)
            {
                return false;
            }

            byte[] ptrBytes = ToByteArray(ptr, length);
            return bytes.SequenceEqual(ptrBytes);
        }
    }
}
