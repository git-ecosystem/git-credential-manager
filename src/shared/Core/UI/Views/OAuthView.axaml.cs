using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GitCredentialManager.UI.Views
{
    public partial class OAuthView : UserControl
    {
        public OAuthView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
