using System.IO;
using GitCredentialManager.Interop.Posix;
using Xunit;
using static GitCredentialManager.Tests.TestUtils;

namespace GitCredentialManager.Tests.Interop.Posix
{
    public class PosixFileSystemTests
    {
        [PlatformFact(Platforms.Posix)]
        public void PosixFileSystem_ResolveSymlinks_FileLinks()
        {
            string baseDir = GetTempDirectory();
            string realPath = CreateFile(baseDir, "realFile.txt");
            string linkPath = CreateFileSymlink(baseDir, "linkFile.txt", realPath);

            string actual = PosixFileSystem.ResolveSymbolicLinks(linkPath);

            Assert.Equal(realPath, actual);
        }

        [PlatformFact(Platforms.Posix)]
        public void PosixFileSystem_ResolveSymlinks_DirectoryLinks()
        {
            //
            // Create a real file inside of a directory that is a symlink
            // to another directory.
            //
            //     /tmp/{uuid}/linkDir/ -> /tmp/{uuid}/realDir/
            //
            string baseDir = GetTempDirectory();
            string realDir = CreateDirectory(baseDir, "realDir");
            string linkDir = CreateDirectorySymlink(baseDir, "linkDir", realDir);
            string filePath = CreateFile(linkDir, "file.txt");

            string actual = PosixFileSystem.ResolveSymbolicLinks(filePath);

            string expected = Path.Combine(realDir, "file.txt");

            Assert.Equal(expected, actual);
        }
    }
}
