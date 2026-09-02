using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Text;
using System.Threading;

namespace GitCredentialManager
{
    /// <summary>
    /// Accepts string messages from multiple threads and dispatches them over a named pipe from a
    /// background thread.
    /// </summary>
    public class Trace2PipeWriter : Trace2Writer
    {
        private const int DefaultMaxQueueSize = 256;

        private readonly string _pipeName;
        private readonly BlockingCollection<string> _queue;

        private Thread _writerThread;
        private NamedPipeClientStream _pipeClient;

        public Trace2PipeWriter(Trace2FormatTarget formatTarget, string pipeName, int maxQueueSize = DefaultMaxQueueSize)
            : base(formatTarget)
        {
            EnsureArgument.NotNullOrWhiteSpace(pipeName, nameof(pipeName));
            EnsureArgument.Positive(maxQueueSize, nameof(maxQueueSize));

            _pipeName = pipeName;
            _queue = new BlockingCollection<string>(new ConcurrentQueue<string>(), boundedCapacity: maxQueueSize);

            Start();
        }

        public override void Write(Trace2Message message)
        {
           _queue.TryAdd(message.ToJson());
        }

        protected override void ReleaseManagedResources()
        {
            Stop();

            _pipeClient?.Dispose();
            _queue.Dispose();
            base.ReleaseManagedResources();

            _pipeClient = null;
            _writerThread = null;
        }

        private void Start()
        {
            try
            {
                _writerThread = new Thread(BackgroundWriterThreadProc)
                {
                    Name = nameof(Trace2PipeWriter),
                    IsBackground = true
                };

                _writerThread.Start();

                // Create a new pipe stream instance
                _pipeClient = new NamedPipeClientStream(
                    ".",
                    _pipeName,
                    PipeDirection.Out,
                    PipeOptions.Asynchronous
                );

                // Specify an instantaneous timeout because we don't want to hold up the
                // background thread loop if the pipe is not available.
                _pipeClient.Connect(timeout: 0);
            }
            catch
            {
                // Start failed. Disable this writer for this run.
                Failed = true;
            }
        }

        private void Stop()
        {
            if (_queue.IsAddingCompleted)
            {
                return;
            }

            // Signal to the queue draining thread that it should drain once more and then terminate.
            _queue.CompleteAdding();
            _writerThread.Join();
            ReleaseManagedResources();
        }

        private void BackgroundWriterThreadProc()
        {
            // Drain the queue of all messages currently in the queue.
            // TryTake() using an infinite timeout will block until either a message is available (returns true)
            // or the queue has been marked as completed _and_ is empty (returns false).
            while (_queue.TryTake(out string message, Timeout.Infinite))
            {
                if (message is not null)
                {
                    WriteMessage(message);
                }
            }
        }

        private void WriteMessage(string message)
        {
            try
            {
                // We should signal the end of each message with a line-feed (LF) character.
                if (!message.EndsWith('\n'))
                {
                    message += '\n';
                }

                byte[] data = Encoding.UTF8.GetBytes(message);
                _pipeClient.Write(data, 0, data.Length);
                _pipeClient.Flush();
            }
            catch
            {
                // We can't send this message for some reason (e.g., broken pipe); we attempt no recovery or retry
                // mechanism but rather disable the writer for the rest of this run.
                Failed = true;
            }
        }
    }
}
