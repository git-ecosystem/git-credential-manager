using System;
using System.Runtime.InteropServices;

namespace GitCredentialManager.Interop.Linux.Native
{
    public static class Gio
    {
        private const string LibraryName = "libgio-2.0.so.0";

        public const string IOErrorDomain = "g-io-error-quark";
        public const string DBusErrorDomain = "g-dbus-error-quark";

        public enum GBusType
        {
            G_BUS_TYPE_STARTER = -1,
            G_BUS_TYPE_NONE = 0,
            G_BUS_TYPE_SYSTEM = 1,
            G_BUS_TYPE_SESSION = 2,
        }

        public enum GIOError
        {
            G_IO_ERROR_CANCELLED = 19,
        }

        public enum GDBusError
        {
            G_DBUS_ERROR_SERVICE_UNKNOWN = 2,
            G_DBUS_ERROR_NAME_HAS_NO_OWNER = 3,
        }

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe IntPtr g_bus_get_sync(
            GBusType bus_type,
            IntPtr cancellable,
            out Glib.GError* error);
    }
}
