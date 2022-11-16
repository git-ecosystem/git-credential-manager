using System.IO;
using GitCredentialManager.Interop.Linux;
using Xunit;
using static GitCredentialManager.Tests.TestUtils;

namespace GitCredentialManager.Tests.Interop.Linux
{
    public class LinuxFileSystemTests
    {
        [PlatformFact(Platforms.Linux)]
        public static void LinuxFileSystem_IsSamePath_SamePath_ReturnsTrue()
        {
            var fs = new LinuxFileSystem();

            string baseDir = GetTempDirectory();
            string fileA = CreateFile(baseDir, "a.file");

            Assert.True(fs.IsSamePath(fileA, fileA));
        }

        [PlatformFact(Platforms.Linux)]
        public static void LinuxFileSystem_IsSamePath_DifferentFile_ReturnsFalse()
        {
            var fs = new LinuxFileSystem();

            string baseDir = GetTempDirectory();
            string fileA = CreateFile(baseDir, "a.file");
            string fileB = CreateFile(baseDir, "b.file");

            Assert.False(fs.IsSamePath(fileA, fileB));
            Assert.False(fs.IsSamePath(fileB, fileA));
        }

        [PlatformFact(Platforms.Linux)]
        public static void LinuxFileSystem_IsSamePath_SameFileDifferentCase_ReturnsFalse()
        {
            var fs = new LinuxFileSystem();

            string baseDir = GetTempDirectory();
            string fileA1 = CreateFile(baseDir, "a.file");
            string fileA2 = Path.Combine(baseDir, "A.file");

            Assert.False(fs.IsSamePath(fileA1, fileA2));
            Assert.False(fs.IsSamePath(fileA2, fileA1));
        }

        [PlatformFact(Platforms.Linux)]
        public static void LinuxFileSystem_IsSamePath_SameFileDifferentPathNormalization_ReturnsTrue()
        {
            var fs = new LinuxFileSystem();

            string baseDir = GetTempDirectory();
            string subDir = CreateDirectory(baseDir, "subDir1", "subDir2");
            string fileA1 = CreateFile(baseDir, "a.file");
            string fileA2 = Path.Combine(subDir, "..", "..", "a.file");

            Assert.True(fs.IsSamePath(fileA1, fileA2));
            Assert.True(fs.IsSamePath(fileA2, fileA1));
        }

        [PlatformFact(Platforms.Linux)]
        public static void LinuxFileSystem_IsSamePath_SameFileViaSymlink_ReturnsTrue()
        {
            var fs = new LinuxFileSystem();

            string baseDir = GetTempDirectory();
            string fileA1 = CreateFile(baseDir, "a.file");
            string fileA2 = CreateFileSymlink(baseDir, "a.link", fileA1);

            Assert.True(fs.IsSamePath(fileA1, fileA2));
            Assert.True(fs.IsSamePath(fileA2, fileA1));
        }

        [PlatformFact(Platforms.Linux)]
        public static void LinuxFileSystem_IsSamePath_SameFileRelativePath_ReturnsTrue()
        {
            var fs = new LinuxFileSystem();

            string baseDir = GetTempDirectory();
            string fileA1 = CreateFile(baseDir, "a.file");
            string fileA2 = "./a.file";

            using (ChangeDirectory(baseDir))
            {
                Assert.True(fs.IsSamePath(fileA1, fileA2));
                Assert.True(fs.IsSamePath(fileA2, fileA1));
            }
        }
    }
}
