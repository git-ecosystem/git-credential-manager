using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GitHub.UI.Views;

public partial class SelectAccountView : UserControl
{
    public SelectAccountView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
