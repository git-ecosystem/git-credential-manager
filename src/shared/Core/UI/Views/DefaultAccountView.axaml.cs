using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GitCredentialManager.UI.Views
{
    public partial class DefaultAccountView : UserControl
    {
        public DefaultAccountView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
