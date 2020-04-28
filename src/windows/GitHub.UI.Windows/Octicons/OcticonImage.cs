using System.Windows;
using System.Windows.Controls;

namespace GitHub.UI.Octicons
{
    public class OcticonImage : Control
    {
        public Octicon Icon
        {
            get { return (Octicon)GetValue(OcticonPath.IconProperty); }
            set { SetValue(OcticonPath.IconProperty, value); }
        }

        public static readonly DependencyProperty IconProperty =
            OcticonPath.IconProperty.AddOwner(typeof(OcticonImage));
    }
}
