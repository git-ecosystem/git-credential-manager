using System;
using System.IO;

namespace GitCredentialManager.Concurrency;

public enum LockScope
{
    /// <summary>
    /// The lock is only valid for the current process.
    /// </summary>
    Process,

    /// <summary>
    /// The lock is valid for all processes running as the current user.
    /// </summary>
    CurrentUser,

    /// <summary>
    /// The lock is valid for all processes on the current machine.
    /// </summary>
    Machine,
}

public interface ILockFactory
{
    ILock CreateLock(LockScope scope, string name);
}

public class LockFactory(IFileSystem fs) : ILockFactory
{
    private readonly IFileSystem _fs = fs ?? throw new ArgumentNullException(nameof(fs));

    public ILock CreateLock(LockScope scope, string name)
    {
        switch (scope)
        {
            case LockScope.Process:
                throw new NotImplementedException();

            case LockScope.CurrentUser:
                throw new NotImplementedException();

            case LockScope.Machine:
                throw new NotImplementedException();

            default:
                throw new ArgumentOutOfRangeException(nameof(scope), scope, "Unknown lock scope");
        }
    }

    private string GetUserLockPath(string name)
    {
        EnsureArgument.NotNullOrWhiteSpace(name, nameof(name));
        if (name is "." or ".." || Path.GetFileName(name) != name ||
            name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("Lock name must be a file name without directory components.", nameof(name));
        }

        string lockDir = Path.Combine(_fs.UserDataDirectoryPath, Constants.LocksDirectoryName);
        return Path.Combine(lockDir, name);
    }
}
