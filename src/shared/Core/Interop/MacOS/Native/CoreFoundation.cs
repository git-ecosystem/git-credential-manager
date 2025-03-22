using System;
using System.Runtime.InteropServices;
using static GitCredentialManager.Interop.MacOS.Native.LibSystem;

namespace GitCredentialManager.Interop.MacOS.Native
{
    public static class CoreFoundation
    {
        private const string CoreFoundationFrameworkLib = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

        public static readonly IntPtr Handle;
        public static readonly IntPtr kCFBooleanTrue;
        public static readonly IntPtr kCFBooleanFalse;

        static CoreFoundation()
        {
            Handle = dlopen(CoreFoundationFrameworkLib, 0);

            kCFBooleanTrue  = GetGlobal(Handle, "kCFBooleanTrue");
            kCFBooleanFalse = GetGlobal(Handle, "kCFBooleanFalse");
        }

        [DllImport(CoreFoundationFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CFArrayCreateMutable(IntPtr allocator, long capacity, IntPtr callbacks);

        [DllImport(CoreFoundationFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void CFArrayInsertValueAtIndex(IntPtr theArray, long idx, IntPtr value);

        [DllImport(CoreFoundationFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern long CFArrayGetCount(IntPtr theArray);

        [DllImport(CoreFoundationFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CFArrayGetValueAtIndex(IntPtr theArray, long idx);

        [DllImport(CoreFoundationFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CFDictionaryCreateMutable(
            IntPtr allocator,
            long capacity,
            IntPtr keyCallBacks,
            IntPtr valueCallBacks);

        [DllImport(CoreFoundationFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void CFDictionaryAddValue(
            IntPtr theDict,
            IntPtr key,
            IntPtr value);

        [DllImport(CoreFoundationFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CFDictionaryGetValue(IntPtr theDict, IntPtr key);

        [DllImport(CoreFoundationFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool CFDictionaryGetValueIfPresent(IntPtr theDict, IntPtr key, out IntPtr value);

        [DllImport(CoreFoundationFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CFStringCreateWithBytes(IntPtr alloc, byte[] bytes, long numBytes,
            CFStringEncoding encoding, bool isExternalRepresentation);

        [DllImport(CoreFoundationFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern long CFStringGetLength(IntPtr theString);

        [DllImport(CoreFoundationFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool CFStringGetCString(IntPtr theString, IntPtr buffer, long bufferSize, CFStringEncoding encoding);

        [DllImport(CoreFoundationFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void CFRetain(IntPtr cf);

        [DllImport(CoreFoundationFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void CFRelease(IntPtr cf);

        [DllImport(CoreFoundationFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CFGetTypeID(IntPtr cf);

        [DllImport(CoreFoundationFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CFStringGetTypeID();

        [DllImport(CoreFoundationFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CFDataGetTypeID();

        [DllImport(CoreFoundationFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CFDictionaryGetTypeID();

        [DllImport(CoreFoundationFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CFArrayGetTypeID();

        [DllImport(CoreFoundationFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CFDataGetBytePtr(IntPtr theData);

        [DllImport(CoreFoundationFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CFDataGetLength(IntPtr theData);
    }

    public enum CFStringEncoding
    {
        kCFStringEncodingUTF8 = 0x08000100,
    }
}
