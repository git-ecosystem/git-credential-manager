using System.Collections.Generic;
using Microsoft.Git.CredentialManager.Interop.Windows;
using Microsoft.Git.CredentialManager.Tests.Objects;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests
{
    public class EnvironmentTests
    {
        [PlatformFact(Platforms.Windows)]
        public void WindowsEnvironment_TryLocateExecutable_NotExists_ReturnFalse()
        {
            string pathVar = @"C:\Users\john.doe\bin;C:\Windows\system32;C:\Windows";
            string execName = "foo.exe";
            var fs = new TestFileSystem();
            var envars = new Dictionary<string, string> {["PATH"] = pathVar};
            var env = new WindowsEnvironment(fs, envars);

            bool actualResult = env.TryLocateExecutable(execName, out string actualPath);

            Assert.False(actualResult);
            Assert.Null(actualPath);
        }

        [PlatformFact(Platforms.Windows)]
        public void WindowsEnvironment_TryLocateExecutable_Windows_Exists_ReturnTrueAndPath()
        {
            string pathVar = @"C:\Users\john.doe\bin;C:\Windows\system32;C:\Windows";
            string execName = "foo.exe";
            string expectedPath = @"C:\Windows\system32\foo.exe";
            var fs = new TestFileSystem
            {
                Files = new Dictionary<string, byte[]>
                {
                    [@"C:\Windows\system32\foo.exe"] = new byte[0],
                }
            };
            var envars = new Dictionary<string, string> {["PATH"] = pathVar};
            var env = new WindowsEnvironment(fs, envars);

            bool actualResult = env.TryLocateExecutable(execName, out string actualPath);

            Assert.True(actualResult);
            Assert.Equal(expectedPath, actualPath);
        }

        [PlatformFact(Platforms.Windows)]
        public void WindowsEnvironment_TryLocateExecutable_Windows_ExistsMultiple_ReturnTrueAndFirstPath()
        {
            string pathVar = @"C:\Users\john.doe\bin;C:\Windows\system32;C:\Windows";
            string execName = "foo.exe";
            string expectedPath = @"C:\Users\john.doe\bin\foo.exe";
            var fs = new TestFileSystem
            {
                Files = new Dictionary<string, byte[]>
                {
                    [@"C:\Users\john.doe\bin\foo.exe"] = new byte[0],
                    [@"C:\Windows\system32\foo.exe"] = new byte[0],
                    [@"C:\Windows\foo.exe"] = new byte[0],
                }
            };
            var envars = new Dictionary<string, string> {["PATH"] = pathVar};
            var env = new WindowsEnvironment(fs, envars);

            bool actualResult = env.TryLocateExecutable(execName, out string actualPath);

            Assert.True(actualResult);
            Assert.Equal(expectedPath, actualPath);
        }
    }
}
