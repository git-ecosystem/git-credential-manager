// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using GitHub.UI.Controls;

namespace GitHub.Authentication.Helper.Controls
{
    public partial class TwoFactorControl : DialogUserControl
    {
        public TwoFactorControl()
        {
            InitializeComponent();
        }

        protected override void SetFocus()
        {
            authenticationCode.TryFocus().Wait(TimeSpan.FromSeconds(1));
        }
    }
}
