// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Windows;
using System.Windows.Controls;

namespace GitHub.Authentication.Helper.Controls.Octicons
{
    public class OcticonImage : Control
    {
        public Octicon Icon
        {
            get { return (Octicon)GetValue(OcticonPath.IconProperty); }
            set { SetValue(OcticonPath.IconProperty, value); }
        }

        public static DependencyProperty IconProperty =
            OcticonPath.IconProperty.AddOwner(typeof(OcticonImage));
    }
}
