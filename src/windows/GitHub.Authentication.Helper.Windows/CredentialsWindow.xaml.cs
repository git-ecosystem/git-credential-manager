// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using GitHub.Authentication.Helper.ViewModels;
using GitHub.UI.Controls;

namespace GitHub.Authentication.Helper
{
    /// <summary>
    /// Interaction logic for CredentialsWindow.xaml
    /// </summary>
    public partial class CredentialsWindow : AuthenticationDialogWindow
    {
        public CredentialsWindow(IntPtr parentHwnd) : base(parentHwnd)
        {
            InitializeComponent();
        }

        public CredentialsWindow() : this(IntPtr.Zero) { }

        public CredentialsViewModel ViewModel => DataContext as CredentialsViewModel;
    }
}
