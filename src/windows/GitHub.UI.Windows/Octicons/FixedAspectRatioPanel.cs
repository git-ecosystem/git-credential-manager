using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace GitHub.UI.Octicons
{
    public class FixedAspectRatioPanel : Panel
    {
        public static readonly DependencyProperty AspectRatioProperty = DependencyProperty.Register(
            "AspectRatio", typeof(double), typeof(FixedAspectRatioPanel), new FrameworkPropertyMetadata(1d)
            {
                AffectsArrange = true,
                AffectsMeasure = true,
                AffectsRender = true
            });

        static FixedAspectRatioPanel()
        {
            Control.HorizontalContentAlignmentProperty.AddOwner(typeof(FixedAspectRatioPanel));
            Control.VerticalContentAlignmentProperty.AddOwner(typeof(FixedAspectRatioPanel));
        }

        [TypeConverter(typeof(AspectRatioConverter))]
        public double AspectRatio
        {
            get { return (double)GetValue(AspectRatioProperty); }
            set { SetValue(AspectRatioProperty, value); }
        }

        public HorizontalAlignment HorizontalContentAlignment
        {
            get { return (HorizontalAlignment)GetValue(Control.HorizontalContentAlignmentProperty); }
            set { SetValue(Control.HorizontalContentAlignmentProperty, value); }
        }

        public VerticalAlignment VerticalContentAlignment
        {
            get { return (VerticalAlignment)GetValue(Control.VerticalContentAlignmentProperty); }
            set { SetValue(Control.VerticalContentAlignmentProperty, value); }
        }

        static Size GetMaxSize(Size availableSize, double constraintAspectRatio)
        {
            double h = availableSize.Height;
            double w = availableSize.Width;

            var availableAspectRatio = w / h;

            if(constraintAspectRatio >= availableAspectRatio) {
                h = w / constraintAspectRatio;
            } else {
                w = h * constraintAspectRatio;
            }

            return new Size(w, h);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var constraint = GetMaxSize(availableSize, AspectRatio);

            var xMax = 0d;
            var yMax = 0d;

            foreach (UIElement element in base.InternalChildren)
            {
                element.Measure(constraint);

                xMax = Math.Max(element.DesiredSize.Width, xMax);
                yMax = Math.Max(element.DesiredSize.Height, yMax);
            }

            return new Size(xMax, yMax);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var constraint = GetMaxSize(finalSize, AspectRatio);

            foreach (UIElement element in base.InternalChildren)
            {
                var pos = GetPosition(finalSize, constraint, HorizontalContentAlignment, VerticalContentAlignment);

                element.Arrange(new Rect(pos, constraint));
            }

            return finalSize;
        }

        static Point GetPosition(Size outer, Size inner, HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment)
        {
            double x = 0;
            double y = 0;

            switch (horizontalAlignment)
            {
                case HorizontalAlignment.Stretch:
                case HorizontalAlignment.Center:
                    x = outer.Width / 2 - inner.Width / 2;
                    break;
                case HorizontalAlignment.Right:
                    x = outer.Width - inner.Width;
                    break;
            }

            switch (verticalAlignment)
            {
                case VerticalAlignment.Stretch:
                case VerticalAlignment.Center:
                    y = outer.Height / 2 - inner.Height / 2;
                    break;
                case VerticalAlignment.Bottom:
                    x = outer.Height - inner.Height;
                    break;
            }

            return new Point(x, y);
        }
    }
}
