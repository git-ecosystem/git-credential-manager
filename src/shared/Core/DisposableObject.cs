using System;

namespace GitCredentialManager
{
    /// <summary>
    /// An object that implements the <see cref="IDisposable"/> interface and the disposable pattern.
    /// </summary>
    public abstract class DisposableObject : IDisposable
    {
        private bool _isDisposed;

        /// <summary>
        /// Throw an exception if the object has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the object has been disposed.</exception>
        protected void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        /// <summary>
        /// Called when unmanaged resources should be released and memory freed.
        /// </summary>
        protected virtual void ReleaseUnmanagedResources() { }

        /// <summary>
        /// Called when managed resources should be released.
        /// </summary>
        protected virtual void ReleaseManagedResources() { }

        /// <summary>
        /// Called when the application is being terminated. Clean up and release any resources.
        /// </summary>
        /// <param name="disposing">True if the instance is being disposed, false if being finalized.</param>
        private void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            ReleaseUnmanagedResources();

            if (disposing)
            {
                ReleaseManagedResources();
            }

            _isDisposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~DisposableObject()
        {
            Dispose(false);
        }
    }
}
