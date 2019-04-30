// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using GitHub.UI.Controls;
using GitHub.UI.ViewModels;

namespace GitHub.Authentication.Helper
{
    public interface IGui
    {
        /// <summary>
        /// Presents the user with `<paramref name="windowCreator"/>` with the `<paramref name="viewModel"/>`.
        /// <para/>
        /// Returns `<see langword="true"/>` if the user completed the dialog; otherwise `<see langword="false"/>` if the user canceled or abandoned the dialog.
        /// </summary>
        /// <param name="viewModel">The view model passed to the presented window.</param>
        /// <param name="windowCreator">Creates the window `<paramref name="viewModel"/>` is passed to.</param>
        bool ShowViewModel(DialogViewModel viewModel, Func<AuthenticationDialogWindow> windowCreator);

        /// <summary>
        /// Present the user with `<paramref name="windowCreator"/>`.
        /// </summary>
        /// <param name="windowCreator">Creates the window.</param>
        void ShowDialogWindow(Func<Window> windowCreator);
    }

    internal class Gui : IGui
    {
        public void ShowDialogWindow(Func<Window> windowCreator)
        {
            StartSTATask(() =>
            {
                EnsureApplicationResources();

                var window = windowCreator();

                window.ShowDialog();
            })
            .Wait();
        }

        public bool ShowViewModel(DialogViewModel viewModel, Func<AuthenticationDialogWindow> windowCreator)
        {
            StartSTATask(() =>
            {
                EnsureApplicationResources();

                var window = windowCreator();

                window.DataContext = viewModel;

                window.ShowDialog();
            })
            .Wait();

            return viewModel.Result == AuthenticationDialogResult.Ok
                && viewModel.IsValid;
        }

        private static void EnsureApplicationResources()
        {
            if (!UriParser.IsKnownScheme("pack"))
            {
                UriParser.Register(new GenericUriParser(GenericUriParserOptions.GenericAuthority), "pack", -1);
            }

            var appResourcesUri = new Uri("pack://application:,,,/GitHub.Authentication.Helper;component/AppResources.xaml", UriKind.RelativeOrAbsolute);

            // If we launch two dialogs in the same process (Credential followed by 2fa), calling new
            // App() throws an exception stating the Application class can't be created twice.
            // Creating an App instance happens to set Application.Current to that instance (it's
            // weird). However, if you don't set the ShutdownMode to OnExplicitShutdown, the second
            // time you launch a dialog, Application.Current is null even in the same process.
            if (Application.Current == null)
            {
                var app = new Application();

                Debug.Assert(Application.Current == app, "Current application not set");

                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = appResourcesUri });
            }
            else
            {
                // Application.Current exists, but what if in the future, some other code created the
                // singleton. Let's make sure our resources are still loaded.
                var resourcesExist = Application.Current.Resources.MergedDictionaries.Any(r => r.Source == appResourcesUri);
                if (!resourcesExist)
                {
                    Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = appResourcesUri });
                }
            }
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
    }
}
