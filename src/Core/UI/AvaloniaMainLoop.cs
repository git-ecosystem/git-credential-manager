using System;
using System.Threading;
using Avalonia;
using Avalonia.Threading;
using AvnDispatcher = Avalonia.Threading.Dispatcher;

namespace GitCredentialManager.UI
{
    /// <summary>
    /// The Avalonia application main loop.
    /// </summary>
    internal class AvaloniaMainLoop : IMainLoop
    {
        private static bool _win32SoftwareRendering;
        private static bool _isStarted;

        /// <summary>
        /// Configure the Avalonia application.
        /// </summary>
        /// <param name="win32SoftwareRendering">True to enable software rendering on Windows, false otherwise.</param>
        /// <exception cref="InvalidOperationException">The application has already been started.</exception>
        public static void Configure(bool win32SoftwareRendering)
        {
            if (_isStarted)
            {
                throw new InvalidOperationException(
                    "Avalonia must be configured before the application is started.");
            }

            _win32SoftwareRendering = win32SoftwareRendering;
        }

        public void Initialize()
        {
            _isStarted = true;

            using (Trace2.StartRegion("ui", "avn_init"))
            {
                AppBuilder appBuilder = AppBuilder.Configure<AvaloniaApp>();

                // Set custom rendering options and modes if required
                if (PlatformUtils.IsWindows() && _win32SoftwareRendering)
                {
                    Trace2.WriteData("ui", "win32/software_rendering", "true");
                    appBuilder.With(new Win32PlatformOptions
                        { RenderingMode = new[] { Win32RenderingMode.Software } });
                }

                appBuilder
                    .UsePlatformDetect()
                    .LogToTrace()
                    .SetupWithoutStarting();
            }
        }

        public void Post(Action work) => AvnDispatcher.UIThread.Post(work, DispatcherPriority.Send);

        public void Run(CancellationToken ct) => AvnDispatcher.UIThread.MainLoop(ct);
    }
}
