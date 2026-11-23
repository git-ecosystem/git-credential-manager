using System;
using System.IO;
using System.Threading.Tasks;
using GitCredentialManager;
using Moq;
using Xunit;

namespace GitHub.Tests;

public class GitHubCredentialLockTests
{
    [Fact]
    public async Task Acquire_PermanentFileErrorsFailWithoutRetry()
    {
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.SetupGet(x => x.UserDataDirectoryPath).Returns($"test-{Guid.NewGuid():N}");
        fileSystem.Setup(x => x.OpenFileStream(
                It.IsAny<string>(), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            .Throws(new DirectoryNotFoundException());
        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => GitHubCredentialLock.AcquireAsync(fileSystem.Object, "github.com")
                .WaitAsync(TimeSpan.FromSeconds(2)));
        fileSystem.Verify(x => x.OpenFileStream(
            It.IsAny<string>(), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None), Times.Once);
    }

    [Fact]
    public async Task Acquire_RetriesNativeSharingConflicts()
    {
        string directory = Directory.CreateTempSubdirectory("gcm-lock-test-").FullName;
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.SetupGet(x => x.UserDataDirectoryPath).Returns(directory);
        fileSystem.Setup(x => x.CreateDirectory(It.IsAny<string>()))
            .Callback<string>(path => Directory.CreateDirectory(path));
        int attempts = 0;
        FileStream blocker = null;
        try
        {
            fileSystem.Setup(x => x.OpenFileStream(
                    It.IsAny<string>(), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                .Returns<string, FileMode, FileAccess, FileShare>((path, mode, access, share) =>
                {
                    if (++attempts == 1)
                    {
                        blocker = File.Open(path, mode, access, share);
                        try
                        {
                            return File.Open(path, mode, access, share);
                        }
                        finally
                        {
                            blocker.Dispose();
                        }
                    }
                    return File.Open(path, mode, access, share);
                });
            using var credentialLock = await GitHubCredentialLock.AcquireAsync(fileSystem.Object, "github.com");
            Assert.Equal(2, attempts);
        }
        finally
        {
            blocker?.Dispose();
            Directory.Delete(directory, recursive: true);
        }
    }
}
