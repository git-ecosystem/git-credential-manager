using System;
using System.Collections.Generic;
using System.Linq;
using GitCredentialManager.Interop.Posix;
using GitCredentialManager.Interop.Windows;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace GitCredentialManager.Tests
{
    public class EnvironmentTests
    {
        private const string WindowsPathVar = @"C:\Users\john.doe\bin;C:\Windows\system32;C:\Windows";
        private const string WindowsExecName = "foo.exe";
        private const string PosixPathVar = "/home/john.doe/bin:/usr/local/bin:/usr/bin:/bin:/usr/sbin:/sbin";
        private const string PosixExecName = "foo";

        [PlatformFact(Platforms.Windows)]
        public void WindowsEnvironment_TryLocateExecutable_NotExists_ReturnFalse()
        {
            var fs = new TestFileSystem();
            var envars = new Dictionary<string, string> {["PATH"] = WindowsPathVar};
            var env = new WindowsEnvironment(fs, envars);

            bool actualResult = env.TryLocateExecutable(WindowsExecName, out string actualPath);

            Assert.False(actualResult);
            Assert.Null(actualPath);
        }

        [PlatformFact(Platforms.Windows)]
        public void WindowsEnvironment_TryLocateExecutable_Exists_ReturnTrueAndPath()
        {
            string expectedPath = @"C:\Windows\system32\foo.exe";
            var fs = new TestFileSystem
            {
                Files = new Dictionary<string, byte[]>
                {
                    [expectedPath] = Array.Empty<byte>()
                }
            };
            var envars = new Dictionary<string, string> {["PATH"] = WindowsPathVar};
            var env = new WindowsEnvironment(fs, envars);

            bool actualResult = env.TryLocateExecutable(WindowsExecName, out string actualPath);

            Assert.True(actualResult);
            Assert.Equal(expectedPath, actualPath);
        }

        [PlatformFact(Platforms.Windows)]
        public void WindowsEnvironment_TryLocateExecutable_ExistsMultiple_ReturnTrueAndFirstPath()
        {
            string expectedPath = @"C:\Users\john.doe\bin\foo.exe";
            var fs = new TestFileSystem
            {
                Files = new Dictionary<string, byte[]>
                {
                    [expectedPath] = Array.Empty<byte>(),
                    [@"C:\Windows\system32\foo.exe"] = Array.Empty<byte>(),
                    [@"C:\Windows\foo.exe"] = Array.Empty<byte>(),
                }
            };
            var envars = new Dictionary<string, string> {["PATH"] = WindowsPathVar};
            var env = new WindowsEnvironment(fs, envars);

            bool actualResult = env.TryLocateExecutable(WindowsExecName, out string actualPath);

            Assert.True(actualResult);
            Assert.Equal(expectedPath, actualPath);
        }

        [PlatformFact(Platforms.Posix)]
        public void PosixEnvironment_TryLocateExecutable_NotExists_ReturnFalse()
        {
            var fs = new TestFileSystem();
            var envars = new Dictionary<string, string> {["PATH"] = PosixPathVar};
            var env = new PosixEnvironment(fs, envars);

            bool actualResult = env.TryLocateExecutable(PosixExecName, out string actualPath);

            Assert.False(actualResult);
            Assert.Null(actualPath);
        }

        [PlatformFact(Platforms.Posix)]
        public void PosixEnvironment_TryLocateExecutable_Exists_ReturnTrueAndPath()
        {
            string expectedPath = "/usr/local/bin/foo";
            var fs = new TestFileSystem
            {
                Files = new Dictionary<string, byte[]>
                {
                    [expectedPath] = Array.Empty<byte>(),
                }
            };
            var envars = new Dictionary<string, string> {["PATH"] = PosixPathVar};
            var env = new PosixEnvironment(fs, envars);

            bool actualResult = env.TryLocateExecutable(PosixExecName, out string actualPath);

            Assert.True(actualResult);
            Assert.Equal(expectedPath, actualPath);
        }

        [PlatformFact(Platforms.Posix)]
        public void PosixEnvironment_TryLocateExecutable_ExistsMultiple_ReturnTrueAndFirstPath()
        {
            string expectedPath = "/home/john.doe/bin/foo";
            var fs = new TestFileSystem
            {
                Files = new Dictionary<string, byte[]>
                {
                    [expectedPath] = Array.Empty<byte>(),
                    ["/usr/local/bin/foo"] = Array.Empty<byte>(),
                    ["/bin/foo"] = Array.Empty<byte>(),
                }
            };
            var envars = new Dictionary<string, string> {["PATH"] = PosixPathVar};
            var env = new PosixEnvironment(fs, envars);

            bool actualResult = env.TryLocateExecutable(PosixExecName, out string actualPath);

            Assert.True(actualResult);
            Assert.Equal(expectedPath, actualPath);
        }

        [PlatformFact(Platforms.MacOS)]
        public void MacOSEnvironment_TryLocateExecutable_Paths_Are_Ignored()
        {
            List<string> pathsToIgnore = new List<string>()
            {
                "/home/john.doe/bin/foo"
            };
            string expectedPath = "/usr/local/bin/foo";

            var fs = new TestFileSystem
            {
                Files = new Dictionary<string, byte[]>
                {
                    [pathsToIgnore.FirstOrDefault()] = Array.Empty<byte>(),
                    [expectedPath] = Array.Empty<byte>(),
                }
            };
            var envars = new Dictionary<string, string> {["PATH"] = PosixPathVar};
            var env = new PosixEnvironment(fs, envars);

            bool actualResult = env.TryLocateExecutable(PosixExecName, pathsToIgnore, out string actualPath);

            Assert.True(actualResult);
            Assert.Equal(expectedPath, actualPath);
        }
        
        [PlatformFact(Platforms.Posix)]
        public void PosixEnvironment_SetEnvironmentVariable_Sets_Expected_Value()
        {
            var variable = "FOO_BAR";
            var value = "baz";
                
            var fs = new TestFileSystem();
            var envars = new Dictionary<string, string>();
            var env = new PosixEnvironment(fs, envars);

            env.SetEnvironmentVariable(variable, value);

            Assert.Contains(env.Variables, item 
                => item.Key.Equals(variable) && item.Value.Equals(value));
        }
        
        [PlatformFact(Platforms.Windows)]
        public void WindowsEnvironment_SetEnvironmentVariable_Sets_Expected_Value()
        {
            var variable = "FOO_BAR";
            var value = "baz";
                
            var fs = new TestFileSystem();
            var envars = new Dictionary<string, string>();
            var env = new WindowsEnvironment(fs, envars);

            env.SetEnvironmentVariable(variable, value);

            Assert.Contains(env.Variables, item 
                => item.Key.Equals(variable) && item.Value.Equals(value));
        }
    }
}
