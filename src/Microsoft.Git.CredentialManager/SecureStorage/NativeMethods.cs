using System;
using System.Runtime.InteropServices;

namespace Microsoft.Git.CredentialManager.SecureStorage
{
    internal static partial class NativeMethods
    {
        public static T[] ToStructArray<T>(byte[] source) where T : struct
        {
            var destination = new T[source.Length / Marshal.SizeOf<T>()];
            GCHandle handle = GCHandle.Alloc(destination, GCHandleType.Pinned);
            try
            {
                IntPtr pointer = handle.AddrOfPinnedObject();
                Marshal.Copy(source, 0, pointer, source.Length);
                return destination;
            }
            finally
            {
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
        }

        public static byte[] ToByteArray(IntPtr ptr, long count)
        {
            var destination = new byte[count];
            Marshal.Copy(ptr, destination, 0, destination.Length);
            return destination;
        }
    }
}
