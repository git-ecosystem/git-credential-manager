using System;
using System.Runtime.InteropServices;
using System.Text;

namespace GitCredentialManager.Interop
{
    /// <summary>
    /// Conversion utilities to convert between .NET strings (UTF-16) and byte arrays (UTF-8).
    /// </summary>
    public static class U8StringConverter
    {
        private const byte NULL = (byte) '\0';

        // Throw only on invalid bytes when converting from managed to native.
        // We shouldn't have invalid managed strings so this would be an error condition.
        // We continue to accept potentially malformed native strings however, because they may come from Git which
        // doesn't technically care about Unicode encoding format compliance.
        private static readonly Encoding NativeEncoding = new UTF8Encoding(false, throwOnInvalidBytes: true);
        private static readonly Encoding ManagedEncoding = new UTF8Encoding(false, throwOnInvalidBytes: false);

        public static unsafe IntPtr ToNative(string str)
        {
            if (str == null)
            {
                return IntPtr.Zero;
            }

            int length = NativeEncoding.GetByteCount(str);

            // +1 for the null terminator byte
            var buffer = (byte*)Marshal.AllocHGlobal(length + 1).ToPointer();

            if (length > 0)
            {
                fixed (char* pValue = str)
                {
                    NativeEncoding.GetBytes(pValue, str.Length, buffer, length);
                }
            }
            buffer[length] = NULL;

            return new IntPtr(buffer);
        }

        public static unsafe string ToManaged(byte* buf)
        {
            byte* end = buf;

            if (buf == null)
            {
                return null;
            }

            if (*buf == NULL)
            {
                return string.Empty;
            }

            while (*end != NULL)
            {
                end++;
            }

            return new string((sbyte*)buf, 0, (int)(end - buf), ManagedEncoding);
        }
    }
}
