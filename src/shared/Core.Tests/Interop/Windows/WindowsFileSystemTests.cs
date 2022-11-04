using System.IO;
using GitCredentialManager.Interop.Windows;
using Xunit;
using static GitCredentialManager.Tests.TestUtils;

namespace GitCredentialManager.Tests.Interop.Windows
{
    public class WindowsFileSystemTests
    {
        [PlatformFact(Platforms.Windows)]
        public static void WindowsFileSystem_IsSamePath_SamePath_ReturnsTrue()
        {
            var fs = new WindowsFileSystem();

            string baseDir = GetTempDirectory();
            string fileA = CreateFile(baseDir, "a.file");

            Assert.True(fs.IsSamePath(fileA, fileA));
        }

        [PlatformFact(Platforms.Windows)]
        public static void WindowsFileSystem_IsSamePath_DifferentFile_ReturnsFalse()
        {
            var fs = new WindowsFileSystem();

            string baseDir = GetTempDirectory();
            string fileA = CreateFile(baseDir, "a.file");
            string fileB = CreateFile(baseDir, "b.file");

            Assert.False(fs.IsSamePath(fileA, fileB));
            Assert.False(fs.IsSamePath(fileB, fileA));
        }

        [PlatformFact(Platforms.Windows)]
        public static void WindowsFileSystem_IsSamePath_SameFileDifferentCase_ReturnsTrue()
        {
            var fs = new WindowsFileSystem();

            string baseDir = GetTempDirectory();
            string fileA1 = CreateFile(baseDir, "a.file");
            string fileA2 = Path.Combine(baseDir, "A.file");

            Assert.True(fs.IsSamePath(fileA1, fileA2));
            Assert.True(fs.IsSamePath(fileA2, fileA1));
        }

        [PlatformFact(Platforms.Windows)]
        public static void WindowsFileSystem_IsSamePath_SameFileDifferentPathNormalization_ReturnsTrue()
        {
            var fs = new WindowsFileSystem();

            string baseDir = GetTempDirectory();
            string subDir = CreateDirectory(baseDir, "subDir1", "subDir2");
            string fileA1 = CreateFile(baseDir, "a.file");
            string fileA2 = Path.Combine(subDir, "..", "..", "a.file");

            Assert.True(fs.IsSamePath(fileA1, fileA2));
            Assert.True(fs.IsSamePath(fileA2, fileA1));
        }

        [PlatformFact(Platforms.Windows)]
        public static void WindowsFileSystem_IsSamePath_SameFileRelativePath_ReturnsTrue()
        {
            var fs = new WindowsFileSystem();

            string baseDir = GetTempDirectory();
            string fileA1 = CreateFile(baseDir, "a.file");
            string fileA2 = @".\a.file";

            using (ChangeDirectory(baseDir))
            {
                Assert.True(fs.IsSamePath(fileA1, fileA2));
                Assert.True(fs.IsSamePath(fileA2, fileA1));
            }
        }
    }
}
