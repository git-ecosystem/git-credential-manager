using System.Windows;
using System.Windows.Input;

namespace GitHub.UI.Dialog
{
    public partial class GitHubDialogWindow : Window
    {
        public GitHubDialogWindow(WindowViewModel viewModel, object content)
        {
            InitializeComponent();

            DataContext = viewModel;
            ContentHolder.Content = content;

            if (viewModel != null)
            {
                viewModel.Accepted += (sender, e) =>
                {
                    this.DialogResult = true;
                    Close();
                };

                viewModel.Canceled += (sender, e) =>
                {
                    this.DialogResult = false;
                    Close();
                };
            }
        }

        void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            Close();
        }

        void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }
    }
}
