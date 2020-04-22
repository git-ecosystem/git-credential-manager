using System;
using System.Runtime.InteropServices;
using System.Security;

namespace GitHub.UI.Controls
{
    /// <summary>
    /// Extension methods for the SecureString type.
    /// </summary>
    internal static class SecureStringExtensions
    {
        /// <summary>
        /// Create a new SecureString from the provided text.
        /// </summary>
        /// <param name="text">The text to create the SecureString from.</param>
        /// <returns>The SecureString with the provided text.</returns>
        internal static SecureString ToSecureString(this string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            SecureString result = new SecureString();
            foreach (char c in text)
            {
                result.AppendChar(c);
            }
            return result;
        }

        /// <summary>
        /// Convert this SecureString to an unsecure string. Only use when necessary!
        /// Code borrowed from https://blogs.msdn.microsoft.com/fpintos/2009/06/12/how-to-properly-convert-securestring-to-string/
        /// </summary>
        /// <param name="secureString">The SecureString to convert.</param>
        /// <returns>The unsecure string version of the SecureString.</returns>
        internal static string ToUnsecureString(this SecureString secureString)
        {
            if (secureString == null)
            {
                throw new ArgumentNullException(nameof(secureString));
            }

            string result = null;
            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(secureString);
                result = Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
            return result;
        }

        /// <summary>
        /// Remove the "length"-number of characters starting at the provided offset from this SecureString.
        /// </summary>
        /// <param name="secureString">The SecureString to remove characters from.</param>
        /// <param name="offset">The offset that characters should begin being removed from.</param>
        /// <param name="length">The number of characters to remove.</param>
        internal static void RemoveAt(this SecureString secureString, int offset, int length)
        {
            for (int i = 0; i < length; ++i)
            {
                secureString.RemoveAt(offset);
            }
        }

        /// <summary>
        /// Insert the provided text into this SecureString at the provided offset.
        /// </summary>
        /// <param name="secureString">The SecureString to insert characters into.</param>
        /// <param name="offset">The offset that characters should begin being inserted into.</param>
        /// <param name="text">The characters to insert.</param>
        internal static void InsertAt(this SecureString secureString, int offset, string text)
        {
            int textLength = text.Length;
            for (int i = 0; i < textLength; ++i)
            {
                secureString.InsertAt(offset + i, text[i]);
            }
        }
    }
}
