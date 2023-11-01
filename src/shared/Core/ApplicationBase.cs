using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager.UI;

namespace GitCredentialManager
{
    public abstract class ApplicationBase : IDisposable
    {
        private static readonly Encoding Utf8NoBomEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        private TextWriter _traceFileWriter;

        protected ICommandContext Context { get; }

        protected ApplicationBase(ICommandContext context)
        {
            EnsureArgument.NotNull(context, nameof(context));

            Context = context;
        }

        public Task<int> RunAsync(string[] args)
        {
            // Launch debugger
            if (Context.Settings.IsDebuggingEnabled)
            {
                Context.Streams.Error.WriteLine("Waiting for debugger to be attached...");
                WaitForDebuggerAttached();

                // Now the debugger is attached, break!
                Debugger.Break();
            }

            // Add the debug tracer if the debugger is attached
            if (Debugger.IsAttached)
            {
                Context.Trace.AddListener(new DebugTraceWriter());
            }

            // Enable tracing
            if (Context.Settings.GetTracingEnabled(out string traceValue))
            {
                if (traceValue.IsTruthy()) // Trace to stderr
                {
                    Context.Trace.AddListener(Context.Streams.Error);
                }
                else if (Path.IsPathRooted(traceValue)) // Trace to a file
                {
                    try
                    {
                        Stream stream = Context.FileSystem.OpenFileStream(traceValue, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                        _traceFileWriter = new StreamWriter(stream, Utf8NoBomEncoding, 4096, leaveOpen: false);

                        Context.Trace.AddListener(_traceFileWriter);
                    }
                    catch (Exception ex)
                    {
                        Context.Streams.Error.WriteLine($"warning: unable to trace to file '{traceValue}': {ex.Message}");
                    }
                }
                else
                {
                    Context.Streams.Error.WriteLine($"warning: unknown value for {Constants.EnvironmentVariables.GcmTrace} '{traceValue}'");
                }
            }

            // Enable sensitive tracing and show warning
            if (Context.Settings.IsSecretTracingEnabled)
            {
                Context.Trace.IsSecretTracingEnabled = true;
                Context.Trace.WriteLine("Tracing of secrets is enabled. Trace output may contain sensitive information.");
            }

            // Set software rendering if defined in settings
            if (Context.Settings.UseSoftwareRendering)
            {
                AvaloniaUi.Initialize(win32SoftwareRendering: true);
            }

            return RunInternalAsync(args);
        }

        protected abstract Task<int> RunInternalAsync(string[] args);

        #region Helpers

        /// <summary>
        /// Wait until a debugger has attached to the currently executing process.
        /// </summary>
        private static void WaitForDebuggerAttached()
        {
            // Attempt to launch the debugger if the OS supports the explicit launching
            if (!Debugger.Launch())
            {
                // The prompt to debug was declined
                return;
            }

            // Some platforms do not support explicit debugger launching..
            // Wait for the debugger to attach - poll & sleep until then
            while (!Debugger.IsAttached)
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _traceFileWriter?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
