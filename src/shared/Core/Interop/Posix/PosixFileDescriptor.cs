using System;
using System.Text;
using GitCredentialManager.Interop.Posix.Native;

namespace GitCredentialManager.Interop.Posix
{
    /// <summary>
    /// Represents a thin wrapper over a POSIX file descriptor.
    /// </summary>
    public class PosixFileDescriptor : DisposableObject
    {
        private readonly int _fd;

        private PosixFileDescriptor()
        {
            PlatformUtils.EnsurePosix();
        }

        public PosixFileDescriptor(string filename, OpenFlags mode) : this()
        {
            _fd = Fcntl.open(filename, mode);
        }

        /// <summary>
        /// True if the file descriptor is invalid (-1), false otherwise.
        /// </summary>
        public bool IsInvalid => _fd == -1;

        public static implicit operator int(PosixFileDescriptor fd)
        {
            return fd._fd;
        }

        /// <summary>
        /// Read <paramref name="count"/> number of bytes into the buffer <paramref name="buf"/> from the file.
        /// </summary>
        /// <param name="buf">Buffer into which to read bytes will be placed.</param>
        /// <param name="count">Maximum number of bytes to read.</param>
        /// <returns>Number of bytes actually read. A value of -1 indicates failure.</returns>
        public int Read(byte[] buf, int count)
        {
            ThrowIfDisposed();
            ThrowIfInvalid();
            return Unistd.read(_fd, buf, count);
        }

        /// <summary>
        /// Write <paramref name="size"/> number of bytes from the buffer <paramref name="buf"/> to the file.
        /// </summary>
        /// <param name="buf">Buffer into which to read bytes will be placed.</param>
        /// <param name="size">Number of bytes from buffer <paramref name="buf"/> to write.</param>
        /// <returns>Number of bytes actually written. A value of -1 indicates failure.</returns>
        public int Write(byte[] buf, int size)
        {
            ThrowIfDisposed();
            ThrowIfInvalid();
            return Unistd.write(_fd, buf, size);
        }

        /// <summary>
        /// Write <paramref name="str"/> as a UTF8 encoded string to the file.
        /// </summary>
        /// <param name="str">String value to write to the file.</param>
        /// <returns>Number of UTF8 bytes written. A value of -1 indicates failure.</returns>
        public int Write(string str)
        {
            byte[] buf = Encoding.UTF8.GetBytes(str);
            return Write(buf, buf.Length);
        }

        protected override void ReleaseUnmanagedResources()
        {
            if (!IsInvalid)
            {
                Unistd.close(_fd);
            }

            base.ReleaseUnmanagedResources();
        }

        private void ThrowIfInvalid()
        {
            if (IsInvalid)
            {
                throw new InvalidOperationException("File descriptor is invalid");
            }
        }
    }
}
