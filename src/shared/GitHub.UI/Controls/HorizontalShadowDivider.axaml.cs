using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GitHub.UI.Controls
{
    public class HorizontalShadowDivider : UserControl
    {
        public HorizontalShadowDivider()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
