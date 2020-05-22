// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Atlassian.Bitbucket.UI.Helpers;

namespace Atlassian.Bitbucket.UI.Controls
{
    /// <summary>
    /// Defines the UI used to prompt users for username/password credentials for Bitbucket accounts.
    /// </summary>
    public partial class CredentialsControl : UserControl
    {
        public CredentialsControl()
        {
            InitializeComponent();

            IsVisibleChanged += (s, e) =>
            {
                if (IsVisible)
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (Action)SetFocus);
                }
            };
        }

        void SetFocus()
        {
            if (string.IsNullOrWhiteSpace(loginTextBox.Text))
            {
                loginTextBox.TryFocus().Wait(TimeSpan.FromSeconds(1));
                Keyboard.Focus(loginTextBox);
            }
            else
            {
                passwordTextBox.TryFocus().Wait(TimeSpan.FromSeconds(1));
                Keyboard.Focus(passwordTextBox);
            }
        }
    }
}
