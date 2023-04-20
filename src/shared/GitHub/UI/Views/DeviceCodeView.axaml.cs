using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GitHub.UI.Views
{
    public class DeviceCodeView : UserControl
    {
        public DeviceCodeView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
