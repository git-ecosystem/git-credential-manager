using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using GitCredentialManager.Interop.Windows.Native;
using GitCredentialManager.UI.Controls;
using GitCredentialManager.UI.ViewModels;
using AvnDispatcher = Avalonia.Threading.Dispatcher;

namespace GitCredentialManager.UI
{
    public static class AvaloniaUi
    {
        private static bool _isAppStarted;
        private static bool _win32SoftwareRendering;

        /// <summary>
        /// Configure the Avalonia application.
        /// </summary>
        /// <param name="win32SoftwareRendering">True to enable software rendering on Windows, false otherwise.</param>
        /// <remarks>
        /// This must be invoked before the Avalonia application loop has started.
        /// </remarks>
        public static void Initialize(bool win32SoftwareRendering)
        {
            if (_isAppStarted)
            {
                throw new InvalidOperationException("Setup must be called before the Avalonia application is started.");
            }

            _win32SoftwareRendering = win32SoftwareRendering;
        }

        public static Task ShowViewAsync(Func<Control> viewFunc, WindowViewModel viewModel, IntPtr parentHandle, CancellationToken ct) =>
            ShowWindowAsync(() => new DialogWindow(viewFunc()), viewModel, parentHandle, ct);

        public static Task ShowViewAsync<TView>(WindowViewModel viewModel, IntPtr parentHandle, CancellationToken ct)
            where TView : Control, new() =>
            ShowWindowAsync(() => new DialogWindow(new TView()), viewModel, parentHandle, ct);

        public static Task ShowWindowAsync(Func<Window> windowFunc, IntPtr parentHandle, CancellationToken ct) =>
            ShowWindowAsync(windowFunc, null, parentHandle, ct);

        public static Task ShowWindowAsync<TWindow>(IntPtr parentHandle, CancellationToken ct)
            where TWindow : Window, new() =>
            ShowWindowAsync(() => new TWindow(), null, parentHandle, ct);

        public static Task ShowWindowAsync<TWindow>(object dataContext, IntPtr parentHandle, CancellationToken ct)
            where TWindow : Window, new() =>
            ShowWindowAsync(() => new TWindow(), dataContext, parentHandle, ct);

        public static Task ShowWindowAsync(Func<Window> windowFunc, object dataContext, IntPtr parentHandle, CancellationToken ct)
        {
            if (!_isAppStarted)
            {
                _isAppStarted = true;

                var appInitialized = new ManualResetEventSlim();

                // Fire and forget the Avalonia app main loop over to our dispatcher (running on the main/entry thread).
                // This action only returns on our dispatcher shutdown.
                Dispatcher.MainThread.Post(appCancelToken =>
                {
                    var appBuilder = AppBuilder.Configure<AvaloniaApp>();

#if NETFRAMEWORK
                    // Set custom rendering options and modes if required
                    if (PlatformUtils.IsWindows() && _win32SoftwareRendering)
                    {
                        appBuilder.With(new Win32PlatformOptions
                            { RenderingMode = new[] { Win32RenderingMode.Software } });
                    }
#endif

                    appBuilder
#if NETFRAMEWORK
                        .UseWin32()
                        .UseSkia()
#else
                        .UsePlatformDetect()
#endif
                        .LogToTrace()
                        .SetupWithoutStarting();

                    appInitialized.Set();

                    // Run the application loop (only exit when the dispatcher is shutting down)
                    AvnDispatcher.UIThread.MainLoop(appCancelToken);
                });

                // Wait for the action posted above to be dequeued from the dispatcher's job queue
                // and for the Avalonia framework (and their dispatcher) to be initialized.
                appInitialized.Wait();
            }

            // Post the window action to the Avalonia dispatcher (which should be running)
            return AvnDispatcher.UIThread.InvokeAsync(
                () => ShowWindowInternal(windowFunc, dataContext, parentHandle, ct),
                DispatcherPriority.Send
            );
        }

        private static Task ShowWindowInternal(Func<Window> windowFunc, object dataContext, IntPtr parentHandle, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<object>();
            Window window = windowFunc();
            window.DataContext = dataContext;
            ct.Register(() =>
                AvnDispatcher.UIThread.InvokeAsync(() => window.Close())
            );
            window.Closed += (s, e) => tcs.SetResult(null);
            window.Show();

            // Avalonia requires a managed "Avalonia.Controls.Window" instance to set the
            // parent/owner window of our window. Since our parent is external and we only
            // have a window handle/ID we must manually parent the window.
            if (parentHandle != IntPtr.Zero)
            {
                SetParentExternal(window, parentHandle);
            }

            // Bring the window in to focus
            window.Activate();
            window.Focus();

            // Workaround an issue where "Activate()" and "Focus()" don't actually
            // cause the window to become the top-most window. Avalonia is correctly
            // calling 'makeKeyAndOrderFront' but this isn't working for some reason.
            if (PlatformUtils.IsMacOS())
            {
                window.Topmost = true;
                window.Topmost = false;
            }

            return tcs.Task;
        }

        private static void SetParentExternal(Window window, IntPtr parentHandle)
        {
            // We only support parenting on the Windows platform at the moment.
            if (!PlatformUtils.IsWindows())
            {
                return;
            }

            IntPtr ourHandle = window.TryGetPlatformHandle()!.Handle;

            // Get the desktop scaling factor from our window instance so we
            // can calculate rects correctly for both our window, and the parent window.
            double scaling = window.RenderScaling;

            // Get our window rect
            var ourRect = new PixelRect(
                PixelPoint.Origin,
                PixelSize.FromSize(window.ClientSize, scaling));

            // Get the parent rect
            if (!(GetWindowRectWin32(parentHandle, scaling) is PixelRect parentRect))
            {
                return;
            }

            // Set the position of our window to the center of the parent.
            PixelRect centerRect = parentRect.CenterRect(ourRect);
            window.Position = centerRect.Position;

            // Tell the platform native windowing system that we wish to parent
            // our window to the external window handle.
            SetWindowParentWin32(ourHandle, parentHandle);
        }

        private static PixelRect? GetWindowRectWin32(IntPtr hwnd, double scaling)
        {
            if (!User32.GetWindowRect(hwnd, out RECT windowRect)) return null;
            var parentPosition = new PixelPoint(windowRect.left, windowRect.top);

            if (!User32.GetClientRect(hwnd, out RECT clientRect)) return null;
            var parentClientSize = new Size(clientRect.right, clientRect.bottom) / scaling;

            return new PixelRect(
                parentPosition,
                PixelSize.FromSize(parentClientSize, scaling));
        }

        private static void SetWindowParentWin32(IntPtr hwnd, IntPtr parentHwnd)
        {
            // Note that the docs say do NOT call this method to set the parent.. call SetParent instead.
            // Avalonia UI itself uses this "incorrect" method, and when experimenting to try and use SetParent
            // (and update the WS_POPUP -> WS_CHILD window style), we get an invisible window.. hmm...
            User32.SetWindowLongPtr(hwnd, (int)WindowLongParam.GWL_HWNDPARENT, parentHwnd);
        }
    }
}
