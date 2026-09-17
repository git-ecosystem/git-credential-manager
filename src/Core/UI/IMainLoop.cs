using System;
using System.Threading;

namespace GitCredentialManager.UI
{
    /// <summary>
    /// A platform main loop that owns the <see cref="Dispatcher"/> thread.
    /// </summary>
    /// <remarks>
    /// All members except <see cref="Post"/> are called on the dispatcher thread.
    /// </remarks>
    internal interface IMainLoop
    {
        /// <summary>
        /// Initialize the main loop on the calling thread, but do not start running it.
        /// </summary>
        /// <remarks>
        /// Once this returns, <see cref="Post"/> must accept work even though <see cref="Run"/>
        /// has not been called yet.
        /// </remarks>
        void Initialize();

        /// <summary>
        /// Post work to the main loop's job queue. Callable from any thread.
        /// </summary>
        /// <param name="work">Work to be run.</param>
        void Post(Action work);

        /// <summary>
        /// Run the main loop, returning only once <paramref name="ct"/> is cancelled.
        /// </summary>
        /// <param name="ct">Token signalling that the main loop should exit.</param>
        void Run(CancellationToken ct);
    }
}
