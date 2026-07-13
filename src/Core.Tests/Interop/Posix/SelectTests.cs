using System.Runtime.InteropServices;
using GitCredentialManager.Interop.Posix.Native;
using Xunit;

namespace GitCredentialManager.Tests.Interop.Posix;

public class SelectTests
{
    [DllImport("libc", SetLastError = true)]
    private static extern int pipe(int[] fds);

    [DllImport("libc", SetLastError = true)]
    private static extern int write(int fd, byte[] buf, int count);

    [DllImport("libc", SetLastError = true)]
    private static extern int close(int fd);

    [PosixFact]
    public void WaitReadable_NoData_TimesOutAndReturnsFalse()
    {
        var fds = new int[2];
        Assert.Equal(0, pipe(fds));
        try
        {
            // Read end has no data: select must honour the timeout and report
            // not-readable. This is the exact behaviour the Escape-disambiguation
            // path depends on (and which macOS poll() fails to provide on a tty).
            bool readable = Select.WaitReadable(fds[0], timeoutMs: 50);
            Assert.False(readable);
        }
        finally
        {
            close(fds[0]);
            close(fds[1]);
        }
    }

    [PosixFact]
    public void WaitReadable_DataAvailable_ReturnsTrue()
    {
        var fds = new int[2];
        Assert.Equal(0, pipe(fds));
        try
        {
            Assert.Equal(1, write(fds[1], new byte[] { 0x1B }, 1));

            bool readable = Select.WaitReadable(fds[0], timeoutMs: 50);
            Assert.True(readable);
        }
        finally
        {
            close(fds[0]);
            close(fds[1]);
        }
    }

    [PosixFact]
    public void WaitReadable_ZeroTimeout_NoData_ReturnsFalse()
    {
        var fds = new int[2];
        Assert.Equal(0, pipe(fds));
        try
        {
            // Zero-timeout poll (the IsKeyAvailable path) must not block and must
            // report not-readable when nothing is buffered.
            Assert.False(Select.WaitReadable(fds[0], timeoutMs: 0));
        }
        finally
        {
            close(fds[0]);
            close(fds[1]);
        }
    }
}
