using System;
using System.IO;
using System.Threading.Tasks;
using GitCredentialManager.Concurrency;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace GitCredentialManager.Tests.Concurrency;

public class LockFactoryTests
{
    [Fact]
    public async Task LockFactory_CurrentUser_UsesItsDataDirectoryAndFilesystem()
    {
        var fs = new TestFileSystem();
        var factory = new LockFactory(fs);
        string directory = Path.Combine(fs.UserDataDirectoryPath, "locks");
        string path = Path.Combine(directory, "test-lock");

        using ILock mutex = factory.CreateLock(LockScope.CurrentUser, "test-lock");
        Assert.IsType<FileLock>(mutex);
        Assert.Contains(directory, fs.Directories);
        Assert.False(fs.Files.ContainsKey(path));

        await mutex.AcquireAsync();
        Assert.True(fs.Files.ContainsKey(path));
        mutex.Dispose();
        Assert.False(fs.Files.ContainsKey(path));
    }

    [Theory]
    [InlineData(LockScope.Process)]
    [InlineData(LockScope.Machine)]
    public void LockFactory_OtherScopes_RemainUnimplemented(LockScope scope)
    {
        var fs = new TestFileSystem();
        var factory = new LockFactory(fs);

        Assert.Throws<NotImplementedException>(() => factory.CreateLock(scope, "test-lock"));
        Assert.Empty(fs.Files);
        Assert.Empty(fs.Directories);
    }

    [Fact]
    public void LockFactory_UnknownScope_Throws()
    {
        var factory = new LockFactory(new TestFileSystem());
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateLock((LockScope)123, "test-lock"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("../outside")]
    [InlineData("nested/lock")]
    public void LockFactory_CurrentUser_InvalidNameCannotEscapeLockDirectory(string name)
    {
        var fs = new TestFileSystem();
        var factory = new LockFactory(fs);

        Assert.ThrowsAny<ArgumentException>(() => factory.CreateLock(LockScope.CurrentUser, name));
        Assert.Empty(fs.Files);
        Assert.Empty(fs.Directories);
    }
}
