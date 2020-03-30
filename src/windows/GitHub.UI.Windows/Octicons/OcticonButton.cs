using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GitHub.UI.Octicons
{
    public class OcticonButton : Button
    {
        public static readonly DependencyProperty IconRotationAngleProperty = DependencyProperty.Register(
            "IconRotationAngle", typeof(double), typeof(OcticonButton),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

        public double IconRotationAngle
        {
            get { return (double)GetValue(IconRotationAngleProperty); }
            set { SetValue(IconRotationAngleProperty, value); }
        }

        public static readonly DependencyProperty DataProperty =
            Path.DataProperty.AddOwner(typeof(OcticonButton));

        public Geometry Data
        {
            get { return (Geometry)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        public static readonly DependencyProperty IconProperty =
            OcticonPath.IconProperty.AddOwner(
                typeof(OcticonButton),
                new FrameworkPropertyMetadata(defaultValue: Octicon.mark_github, flags:
                    FrameworkPropertyMetadataOptions.AffectsArrange |
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    propertyChangedCallback: OnIconChanged));

        public Octicon Icon
        {
            get { return (Octicon)GetValue(OcticonPath.IconProperty); }
            set { SetValue(OcticonPath.IconProperty, value); }
        }

        static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.SetValue(DataProperty, OcticonPath.GetGeometryForIcon((Octicon)e.NewValue));
        }
    }
}
