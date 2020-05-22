// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Windows;
using System.Windows.Input;
using Microsoft.Git.CredentialManager.UI.ViewModels;

namespace Microsoft.Git.CredentialManager.UI.Controls
{
    public partial class DialogWindow : Window
    {
        public DialogWindow(WindowViewModel viewModel, object content)
        {
            InitializeComponent();

            DataContext = viewModel;
            ContentHolder.Content = content;

            if (viewModel != null)
            {
                viewModel.Accepted += (sender, e) =>
                {
                    DialogResult = true;
                    Close();
                };

                viewModel.Canceled += (sender, e) =>
                {
                    DialogResult = false;
                    Close();
                };
            }
        }

        public WindowViewModel ViewModel => (WindowViewModel) DataContext;

        private void CloseButton_Click(object sender, RoutedEventArgs e) => ViewModel.Cancel();

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }
    }
}
