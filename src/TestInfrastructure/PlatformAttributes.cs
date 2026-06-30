using System.Runtime.InteropServices;
using Xunit;

namespace GitCredentialManager.Tests
{
    public class WindowsFactAttribute : FactAttribute
    {
        public WindowsFactAttribute()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Skip = "Test not supported on this platform.";
            }
        }
    }

    public class MacOSFactAttribute : FactAttribute
    {
        public MacOSFactAttribute()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Skip = "Test not supported on this platform.";
            }
        }
    }

    public class LinuxFactAttribute : FactAttribute
    {
        public LinuxFactAttribute()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Skip = "Test not supported on this platform.";
            }
        }
    }

    public class PosixFactAttribute : FactAttribute
    {
        public PosixFactAttribute()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX) &&
                !RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Skip = "Test not supported on this platform.";
            }
        }
    }

    public class WindowsTheoryAttribute : TheoryAttribute
    {
        public WindowsTheoryAttribute()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Skip = "Test not supported on this platform.";
            }
        }
    }

    public class MacOSTheoryAttribute : TheoryAttribute
    {
        public MacOSTheoryAttribute()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Skip = "Test not supported on this platform.";
            }
        }
    }

    public class LinuxTheoryAttribute : TheoryAttribute
    {
        public LinuxTheoryAttribute()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Skip = "Test not supported on this platform.";
            }
        }
    }

    public class PosixTheoryAttribute : TheoryAttribute
    {
        public PosixTheoryAttribute()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX) &&
                !RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Skip = "Test not supported on this platform.";
            }
        }
    }
}
