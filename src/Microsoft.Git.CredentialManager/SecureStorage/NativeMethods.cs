using System;
using System.Runtime.InteropServices;

namespace Microsoft.Git.CredentialManager.SecureStorage
{
    internal static partial class NativeMethods
    {
        public static byte[] ToByteArray(IntPtr ptr, long count)
        {
            var destination = new byte[count];
            Marshal.Copy(ptr, destination, 0, destination.Length);
            return destination;
        }
    }
}
