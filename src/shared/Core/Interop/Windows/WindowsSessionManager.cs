using System;
using GitCredentialManager.Interop.Windows.Native;

namespace GitCredentialManager.Interop.Windows
{
    public class WindowsSessionManager : SessionManager
    {
        public WindowsSessionManager(IEnvironment env, IFileSystem fs) : base(env, fs)
        {
            PlatformUtils.EnsureWindows();
        }

        public override unsafe bool IsDesktopSession
        {
            get
            {
                // Environment.UserInteractive is hard-coded to return true for POSIX and Windows platforms on .NET Core 2.x and 3.x.
                // In .NET 5 the implementation on Windows has been 'fixed', but still POSIX versions always return true.
                //
                // This code is lifted from the .NET 5 targeting dotnet/runtime implementation for Windows:
                // https://github.com/dotnet/runtime/blob/cf654f08fb0078a96a4e414a0d2eab5e6c069387/src/libraries/System.Private.CoreLib/src/System/Environment.Windows.cs#L125-L145

                // Per documentation of GetProcessWindowStation, this handle should not be closed
                IntPtr handle = User32.GetProcessWindowStation();
                if (handle != IntPtr.Zero)
                {
                    USEROBJECTFLAGS flags = default;
                    uint dummy = 0;
                    if (User32.GetUserObjectInformation(handle, User32.UOI_FLAGS, &flags,
                        (uint) sizeof(USEROBJECTFLAGS), ref dummy))
                    {
                        return (flags.dwFlags & User32.WSF_VISIBLE) != 0;
                    }
                }

                // If we can't determine, return true optimistically
                // This will include cases like Windows Nano which do not expose WindowStations
                return true;
            }
        }
    }
}
