// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using GitHub.Authentication.Helper.ViewModels;
using GitHub.UI.Controls;

namespace GitHub.Authentication.Helper
{
    public partial class TwoFactorWindow : AuthenticationDialogWindow
    {
        public TwoFactorWindow(IntPtr parentHwnd)
            : base(parentHwnd)
        {
            InitializeComponent();
        }

        public TwoFactorWindow() : this(IntPtr.Zero) { }

        public TwoFactorViewModel ViewModel => DataContext as TwoFactorViewModel;
    }
}
