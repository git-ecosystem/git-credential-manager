// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using GitHub.UI.ViewModels;

namespace GitHub.UI.Controls
{
    public abstract class AuthenticationDialogWindow : Window
    {
        protected AuthenticationDialogWindow(IntPtr parentHwnd)
        {
            DataContextChanged += (s, e) =>
            {
                if (e.OldValue is ViewModel oldViewModel)
                {
                    oldViewModel.PropertyChanged -= HandleDialogResult;
                }

                DataContext = e.NewValue;

                if (DataContext is ViewModel newViewModel)
                {
                    newViewModel.PropertyChanged += HandleDialogResult;
                }
            };

            new WindowInteropHelper(this).Owner = parentHwnd;
        }

        protected AuthenticationDialogWindow() : this(IntPtr.Zero) { }

        private void HandleDialogResult(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DialogViewModel.Result) &&
                sender is DialogViewModel viewModel && viewModel.Result != AuthenticationDialogResult.None)
            {
                Close();
            }
        }
    }
}
