// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using GitHub.UI.Controls;
using GitHub.UI.Helpers;

namespace GitHub.Authentication.Helper.Controls
{
    /// <summary>
    /// Interaction logic for CredentialsControl.xaml
    /// </summary>
    public partial class CredentialsControl : DialogUserControl
    {
        public CredentialsControl()
        {
            InitializeComponent();
        }

        protected override void SetFocus()
        {
            loginTextBox.TryFocus().Wait(TimeSpan.FromSeconds(1));
        }
    }
}
