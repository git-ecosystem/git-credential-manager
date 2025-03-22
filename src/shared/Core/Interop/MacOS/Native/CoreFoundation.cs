using System;
using System.Collections.Generic;
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
        public static extern IntPtr CFStringCreateWithCString(IntPtr alloc, string cStr, CFStringEncoding encoding);

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
        public static extern int CFNumberGetTypeID();

        [DllImport(CoreFoundationFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CFDataGetBytePtr(IntPtr theData);

        [DllImport(CoreFoundationFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CFDataGetLength(IntPtr theData);

        [DllImport(CoreFoundationFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CFPreferencesCopyAppValue(IntPtr key, IntPtr appID);

        [DllImport(CoreFoundationFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool CFNumberGetValue(IntPtr number, CFNumberType theType, out IntPtr valuePtr);

        [DllImport(CoreFoundationFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CFDictionaryGetKeysAndValues(IntPtr theDict, IntPtr[] keys, IntPtr[] values);

        [DllImport(CoreFoundationFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern long CFDictionaryGetCount(IntPtr theDict);

        public static string CFStringToString(IntPtr cfString)
        {
            if (cfString == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(cfString));
            }

            if (CFGetTypeID(cfString) != CFStringGetTypeID())
            {
                throw new InvalidOperationException("Object is not a CFString.");
            }

            long length = CFStringGetLength(cfString);
            IntPtr buffer = Marshal.AllocHGlobal((int)length + 1);

            try
            {
                if (!CFStringGetCString(cfString, buffer, length + 1, CFStringEncoding.kCFStringEncodingUTF8))
                {
                    throw new InvalidOperationException("Failed to convert CFString to C string.");
                }

                return Marshal.PtrToStringAnsi(buffer);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        public static int CFNumberToInt32(IntPtr cfNumber)
        {
            if (cfNumber == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(cfNumber));
            }

            if (CFGetTypeID(cfNumber) != CFNumberGetTypeID())
            {
                throw new InvalidOperationException("Object is not a CFNumber.");
            }

            if (!CFNumberGetValue(cfNumber, CFNumberType.kCFNumberIntType, out IntPtr valuePtr))
            {
                throw new InvalidOperationException("Failed to convert CFNumber to Int32.");
            }

            return valuePtr.ToInt32();
        }

        public static IDictionary<string, string> CFDictionaryToDictionary(IntPtr cfDict)
        {
            if (cfDict == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(cfDict));
            }

            if (CFGetTypeID(cfDict) != CFDictionaryGetTypeID())
            {
                throw new InvalidOperationException("Object is not a CFDictionary.");
            }

            int count = (int)CFDictionaryGetCount(cfDict);
            var keys = new IntPtr[count];
            var values = new IntPtr[count];

            CFDictionaryGetKeysAndValues(cfDict, keys, values);

            var dict = new Dictionary<string, string>(capacity: count);
            for (int i = 0; i < count; i++)
            {
                string keyStr = CFStringToString(keys[i])!;
                string valueStr = CFStringToString(values[i]);

                dict[keyStr] = valueStr;
            }

            return dict;
        }
    }

    public enum CFStringEncoding
    {
        kCFStringEncodingUTF8 = 0x08000100,
    }

    public enum CFNumberType
    {
        kCFNumberSInt8Type = 1,
        kCFNumberSInt16Type = 2,
        kCFNumberSInt32Type = 3,
        kCFNumberSInt64Type = 4,
        kCFNumberFloat32Type = 5,
        kCFNumberFloat64Type = 6,
        kCFNumberCharType = 7,
        kCFNumberShortType = 8,
        kCFNumberIntType = 9,
        kCFNumberLongType = 10,
        kCFNumberLongLongType = 11,
        kCFNumberFloatType = 12,
        kCFNumberDoubleType = 13,
        kCFNumberCFIndexType = 14,
        kCFNumberNSIntegerType = 15,
        kCFNumberCGFloatType = 16
    }
}
