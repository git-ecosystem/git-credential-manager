using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Git.CredentialManager.UI.Controls;
using Microsoft.Git.CredentialManager.UI.ViewModels;

namespace Microsoft.Git.CredentialManager.UI
{
    public static class Gui
    {
        /// <summary>
        /// Present the user with a <see cref="Window"/>.
        /// </summary>
        /// <param name="windowCreator"><see cref="Window"/> factory.</param>
        /// <param name="parentHwnd">Parent window handle.</param>
        /// <returns>
        /// Returns `<see langword="true"/>` if the user completed the dialog; otherwise `<see langword="false"/>`
        /// if the user canceled or abandoned the dialog.
        /// </returns>
        public static Task ShowWindow(Func<Window> windowCreator, IntPtr parentHwnd)
        {
            return StartSTATask(() => ShowDialog(windowCreator(), parentHwnd));
        }

        /// <summary>
        /// Present the user with a <see cref="DialogWindow"/>.
        /// </summary>
        /// <returns>
        /// Returns `<see langword="true"/>` if the user completed the dialog and the view model is valid;
        /// otherwise `<see langword="false"/>` if the user canceled or abandoned the dialog, or the view
        /// model is invalid.
        /// </returns>
        /// <param name="viewModel">Window view model.</param>
        /// <param name="contentCreator">Window content factory.</param>
        /// <param name="parentHwnd">Parent window handle.</param>
        public static Task ShowDialogWindow(WindowViewModel viewModel, Func<UserControl> contentCreator, IntPtr parentHwnd)
        {
            return ShowWindow(() => new DialogWindow(contentCreator()) { DataContext = viewModel }, parentHwnd);
        }

        private static Task StartSTATask(Action action)
        {
            var completionSource = new TaskCompletionSource<object>();
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                    completionSource.SetResult(null);
                }
                catch (Exception e)
                {
                    completionSource.SetException(e);
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            return completionSource.Task;
        }

        public static bool? ShowDialog(Window window, IntPtr parentHwnd)
        {
            // Zero is not a valid window handle
            if (parentHwnd == IntPtr.Zero)
            {
                return window.ShowDialog();
            }

            // Set the parent window handle and ensure the dialog starts in the correct location
            new System.Windows.Interop.WindowInteropHelper(window).Owner = parentHwnd;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            const int ERROR_INVALID_WINDOW_HANDLE = 1400;

            try
            {
                return window.ShowDialog();
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == ERROR_INVALID_WINDOW_HANDLE)
            {
                // The window handle given was invalid - clear the owner and show the dialog centered on the screen
                window.Owner = null;
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                return window.ShowDialog();
            }
        }
    }
}
