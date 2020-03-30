using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GitHub.UI.Octicons
{
    /// <summary>
    /// Represent a raw path with no transformation. Uses the coordinate system from the octicon/svg files which
    /// are all drawn on a 1024px high canvas with variable width. If you're just after the shape this control
    /// can be used with Stretch=Uniform. If you're looking for an accurately scaled octicon correctly position
    /// you'll have to explicitly set the height of the path to 1024 and wrap it in a viewbox to scale it down
    /// to the size you want.
    /// </summary>
    public class OcticonPath : Shape
    {
        readonly static Lazy<Dictionary<Octicon, Lazy<Geometry>>> cache =
            new Lazy<Dictionary<Octicon, Lazy<Geometry>>>(PrepareCache);

        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
            "Icon", typeof(Octicon), typeof(OcticonPath),
            new FrameworkPropertyMetadata(defaultValue: Octicon.mark_github, flags:
                FrameworkPropertyMetadataOptions.AffectsArrange |
                FrameworkPropertyMetadataOptions.AffectsMeasure |
                FrameworkPropertyMetadataOptions.AffectsRender,
                propertyChangedCallback: OnIconChanged
            )
        );

        public Octicon Icon
        {
            get { return (Octicon)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        protected override Geometry DefiningGeometry
        {
            get { return GetGeometryForIcon(this.Icon); }
        }

        public static Geometry GetGeometryForIcon(Octicon icon)
        {
            var c = cache.Value;
            Lazy<Geometry> g;

            if (c.TryGetValue(icon, out g))
                return g.Value;

            throw new ArgumentException(
                String.Format(CultureInfo.InvariantCulture, "Unknown Octicon: {0}", icon), nameof(icon));
        }

        // Initializes the cache dictionary with lazy entries for all available octicons
        static Dictionary<Octicon, Lazy<Geometry>> PrepareCache()
        {
            return Enum.GetValues(typeof(Octicon))
                .Cast<Octicon>()
                .ToDictionary(icon => icon, icon => new Lazy<Geometry>(() => LoadGeometry(icon), LazyThreadSafetyMode.None));
        }

        static Geometry LoadGeometry(Octicon icon)
        {
            var name = Enum.GetName(typeof(Octicon), icon);

            if (name == "lock")
                name = "_lock";

            var pathData = OcticonPaths.ResourceManager.GetString(name, CultureInfo.InvariantCulture);

            if (pathData == null)
            {
                throw new ArgumentException("Could not find octicon geometry for '" + name + "'");
            }

            var path = PathGeometry.CreateFromGeometry(Geometry.Parse(pathData));

            if (path.CanFreeze)
                path.Freeze();

            return path;
        }
        static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.SetValue(Path.DataProperty, OcticonPath.GetGeometryForIcon((Octicon)e.NewValue));
        }
    }
}