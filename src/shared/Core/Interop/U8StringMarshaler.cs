using System;
using System.Runtime.InteropServices;

namespace GitCredentialManager.Interop
{
    /// <summary>
    /// Marshaler for converting between .NET strings (UTF-16) and byte arrays (UTF-8).
    /// Uses <seealso cref="U8StringConverter"/> internally.
    /// </summary>
    public class U8StringMarshaler : ICustomMarshaler
    {
        // We need to clean up strings that we marshal to native, but should not clean up strings that
        // we marshal to managed.
        private static readonly U8StringMarshaler NativeInstance = new U8StringMarshaler(true);
        private static readonly U8StringMarshaler ManagedInstance = new U8StringMarshaler(false);

        private readonly bool _cleanup;

        public const string NativeCookie  = "U8StringMarshaler.Native";
        public const string ManagedCookie = "U8StringMarshaler.Managed";

        public static ICustomMarshaler GetInstance(string cookie)
        {
            switch (cookie)
            {
                case NativeCookie:
                    return NativeInstance;
                case ManagedCookie:
                    return ManagedInstance;
                default:
                    throw new ArgumentException("Invalid marshaler cookie");
            }
        }

        private U8StringMarshaler(bool cleanup)
        {
            _cleanup = cleanup;
        }

        public int GetNativeDataSize()
        {
            return -1;
        }

        public IntPtr MarshalManagedToNative(object value)
        {
            switch (value)
            {
                case null:
                    return IntPtr.Zero;
                case string str:
                    return U8StringConverter.ToNative(str);
                default:
                    throw new MarshalDirectiveException("Cannot marshal a non-string");
            }
        }

        public unsafe object MarshalNativeToManaged(IntPtr ptr)
        {
            return U8StringConverter.ToManaged((byte*) ptr);
        }

        public void CleanUpManagedData(object value)
        {
        }

        public virtual void CleanUpNativeData(IntPtr ptr)
        {
            if (ptr != IntPtr.Zero && _cleanup)
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
    }
}
