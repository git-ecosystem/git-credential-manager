using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using GitHub.UI.ViewModels;

namespace GitHub.UI.Views;

public partial class SelectAccountView : UserControl
{
    public SelectAccountView()
    {
        InitializeComponent();
    }

    private void ListBox_OnDoubleTapped(object sender, TappedEventArgs e)
    {
        if (DataContext is SelectAccountViewModel { SelectedAccount: not null } vm)
        {
            vm.ContinueCommand.Execute(null);
        }
    }
}
