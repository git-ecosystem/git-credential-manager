// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Windows;
using System.Windows.Controls;

namespace GitHub.Authentication.Helper.Controls.Buttons
{
    public class OcticonButton : Button
    {
        public static readonly DependencyProperty IconRotationAngleProperty = DependencyProperty.Register(
            nameof(IconRotationAngle), typeof(double), typeof(OcticonButton),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

        public double IconRotationAngle
        {
            get { return (double)GetValue(IconRotationAngleProperty); }
            set { SetValue(IconRotationAngleProperty, value); }
        }
    }
}
