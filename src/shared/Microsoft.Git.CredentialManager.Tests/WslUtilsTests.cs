using System;
using System.Diagnostics;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests
{
    public class WslUtilsTests
    {
        [PlatformTheory(Platforms.Windows)]
        [InlineData(null, false)]
        [InlineData(@"", false)]
        [InlineData(@" ", false)]
        [InlineData(@"\", false)]
        [InlineData(@"wsl", false)]
        [InlineData(@"\wsl\ubuntu\home", false)]
        [InlineData(@"\\wsl\ubuntu\home", false)]
        [InlineData(@"\wsl$\ubuntu\home", false)]
        [InlineData(@"wsl$\ubuntu\home", false)]
        [InlineData(@"//wsl$/ubuntu/home", false)]
        [InlineData(@"\\wsl", false)]
        [InlineData(@"\\wsl$", false)]
        [InlineData(@"\\wsl$\", false)]
        [InlineData(@"\\wsl$\ubuntu", true)]
        [InlineData(@"\\wsl$\ubuntu\", true)]
        [InlineData(@"\\wsl$\ubuntu\home", true)]
        [InlineData(@"\\WSL$\UBUNTU\home", true)]
        [InlineData(@"\\wsl$\ubuntu\home\", true)]
        [InlineData(@"\\wsl$\openSUSE-42\home", true)]
        public void WslUtils_IsWslPath(string path, bool expected)
        {
            bool actual = WslUtils.IsWslPath(path);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(@"\\wsl$\ubuntu", "ubuntu", "/")]
        [InlineData(@"\\wsl$\ubuntu\", "ubuntu", "/")]
        [InlineData(@"\\wsl$\ubuntu\home", "ubuntu", "/home")]
        [InlineData(@"\\wsl$\ubuntu\HOME", "ubuntu", "/HOME")]
        [InlineData(@"\\wsl$\UBUNTU\home", "UBUNTU", "/home")]
        [InlineData(@"\\wsl$\ubuntu\home\", "ubuntu", "/home/")]
        [InlineData(@"\\wsl$\openSUSE-42\home", "openSUSE-42", "/home")]
        public void WslUtils_ConvertToDistroPath(string path, string expectedDistro, string expectedPath)
        {
            string actualPath = WslUtils.ConvertToDistroPath(path, out string actualDistro);
            Assert.Equal(expectedPath, actualPath);
            Assert.Equal(expectedDistro, actualDistro);
        }

        [PlatformTheory(Platforms.Windows)]
        [InlineData(null)]
        [InlineData(@"")]
        [InlineData(@" ")]
        [InlineData(@"\")]
        [InlineData(@"wsl")]
        [InlineData(@"\wsl\ubuntu\home")]
        [InlineData(@"\\wsl\ubuntu\home")]
        [InlineData(@"\wsl$\ubuntu\home")]
        [InlineData(@"wsl$\ubuntu\home")]
        [InlineData(@"//wsl$/ubuntu/home")]
        [InlineData(@"\\wsl")]
        [InlineData(@"\\wsl$")]
        [InlineData(@"\\wsl$\")]
        public void WslUtils_ConvertToDistroPath_Invalid_ThrowsException(string path)
        {
            Assert.Throws<ArgumentException>(() => WslUtils.ConvertToDistroPath(path, out _));
        }

        [PlatformFact(Platforms.Windows)]
        public void WslUtils_CreateWslProcess()
        {
            const string distribution = "ubuntu";
            const string command = "/usr/lib/git-core/git version";

            string expectedFileName = WslUtils.GetWslPath();
            string expectedArgs = $"--distribution {distribution} --exec {command}";

            Process process = WslUtils.CreateWslProcess(distribution, command);

            Assert.NotNull(process);
            Assert.Equal(expectedArgs, process.StartInfo.Arguments);
            Assert.Equal(expectedFileName, process.StartInfo.FileName);
            Assert.True(process.StartInfo.RedirectStandardInput);
            Assert.True(process.StartInfo.RedirectStandardOutput);
            Assert.True(process.StartInfo.RedirectStandardError);
            Assert.False(process.StartInfo.UseShellExecute);
        }

        [PlatformFact(Platforms.Windows)]
        public void WslUtils_CreateWslProcess_WorkingDirectory()
        {
            const string distribution = "ubuntu";
            const string command = "/usr/lib/git-core/git version";
            const string expectedWorkingDirectory = @"C:\Projects\";

            string expectedFileName = WslUtils.GetWslPath();
            string expectedArgs = $"--distribution {distribution} --exec {command}";

            Process process = WslUtils.CreateWslProcess(distribution, command, expectedWorkingDirectory);

            Assert.NotNull(process);
            Assert.Equal(expectedArgs, process.StartInfo.Arguments);
            Assert.Equal(expectedFileName, process.StartInfo.FileName);
            Assert.True(process.StartInfo.RedirectStandardInput);
            Assert.True(process.StartInfo.RedirectStandardOutput);
            Assert.True(process.StartInfo.RedirectStandardError);
            Assert.False(process.StartInfo.UseShellExecute);
            Assert.Equal(expectedWorkingDirectory, process.StartInfo.WorkingDirectory);
        }
    }
}
