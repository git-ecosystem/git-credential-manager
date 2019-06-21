// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using GitHub.Authentication.Helper.Controls.Octicons;

namespace GitHub.Authentication.Helper.Controls.Buttons
{
    public class OcticonCircleButton : OcticonButton
    {
        public static readonly DependencyProperty ShowSpinnerProperty = DependencyProperty.Register(
            nameof(ShowSpinner), typeof(bool), typeof(OcticonCircleButton));

        public static readonly DependencyProperty IconForegroundProperty = DependencyProperty.Register(
            nameof(IconForeground), typeof(Brush), typeof(OcticonCircleButton));

        public static readonly DependencyProperty ActiveBackgroundProperty = DependencyProperty.Register(
            nameof(ActiveBackground), typeof(Brush), typeof(OcticonCircleButton));

        public static readonly DependencyProperty ActiveForegroundProperty = DependencyProperty.Register(
            nameof(ActiveForeground), typeof(Brush), typeof(OcticonCircleButton));

        public static readonly DependencyProperty PressedBackgroundProperty = DependencyProperty.Register(
            nameof(PressedBackground), typeof(Brush), typeof(OcticonCircleButton));

        public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register(
            nameof(IconSize), typeof(double), typeof(OcticonCircleButton), new FrameworkPropertyMetadata(16d,
                FrameworkPropertyMetadataOptions.AffectsArrange |
                FrameworkPropertyMetadataOptions.AffectsMeasure |
                FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
            nameof(Icon), typeof(Octicon), typeof(OcticonCircleButton),
            new FrameworkPropertyMetadata(defaultValue: Octicon.mark_github, flags:
                FrameworkPropertyMetadataOptions.AffectsArrange |
                FrameworkPropertyMetadataOptions.AffectsMeasure |
                FrameworkPropertyMetadataOptions.AffectsRender,
                propertyChangedCallback: OnIconChanged
            )
        );

        public bool ShowSpinner
        {
            get { return (bool)GetValue(ShowSpinnerProperty); }
            set { SetValue(ShowSpinnerProperty, value); }
        }

        public Brush IconForeground
        {
            get { return (Brush)GetValue(IconForegroundProperty); }
            set { SetValue(IconForegroundProperty, value); }
        }

        public Brush ActiveBackground
        {
            get { return (Brush)GetValue(ActiveBackgroundProperty); }
            set { SetValue(ActiveBackgroundProperty, value); }
        }

        public Brush ActiveForeground
        {
            get { return (Brush)GetValue(ActiveForegroundProperty); }
            set { SetValue(ActiveForegroundProperty, value); }
        }

        public Brush PressedBackground
        {
            get { return (Brush)GetValue(PressedBackgroundProperty); }
            set { SetValue(PressedBackgroundProperty, value); }
        }

        public double IconSize
        {
            get { return (double)GetValue(IconSizeProperty); }
            set { SetValue(IconSizeProperty, value); }
        }

        public Octicon Icon
        {
            get { return (Octicon)GetValue(OcticonPath.IconProperty); }
            set { SetValue(OcticonPath.IconProperty, value); }
        }

        public Geometry Data
        {
            get { return (Geometry)GetValue(Path.DataProperty); }
            set { SetValue(Path.DataProperty, value); }
        }

        static OcticonCircleButton()
        {
            Path.DataProperty.AddOwner(typeof(OcticonCircleButton));
        }

        private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.SetValue(Path.DataProperty, OcticonPath.GetGeometryForIcon((Octicon)e.NewValue));
        }
    }
}
