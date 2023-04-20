using System.Windows;
using GitCredentialManager;
using GitHub.UI.ViewModels;
using GitHub.UI.Views;
using GitCredentialManager.Interop.Windows;
using GitCredentialManager.UI.Controls;

namespace GitHub.UI.Controls
{
    public partial class TesterWindow : Window
    {
        private readonly WindowsEnvironment _environment = new WindowsEnvironment(new WindowsFileSystem());
        private readonly IProcessManager _processManager;

        public TesterWindow()
        {
            ICommandContext commandContext = new CommandContext();
            _processManager = new ProcessManager(new Trace2(commandContext));
            InitializeComponent();
        }

        private void ShowCredentials(object sender, RoutedEventArgs e)
        {
            var vm = new CredentialsViewModel(_environment, _processManager)
            {
                ShowBrowserLogin = useBrowser.IsChecked ?? false,
                ShowDeviceLogin = useDevice.IsChecked ?? false,
                ShowTokenLogin = usePat.IsChecked ?? false,
                ShowBasicLogin = useBasic.IsChecked ?? false,
                EnterpriseUrl = enterpriseUrl.Text,
                UserName = username.Text
            };
            var view = new CredentialsView();
            var window = new WpfDialogWindow(view) { DataContext = vm };
            window.ShowDialog();
        }

        private void ShowTwoFactorCode(object sender, RoutedEventArgs e)
        {
            var vm = new TwoFactorViewModel(_environment, _processManager)
            {
                IsSms = twoFaSms.IsChecked ?? false,
            };
            var view = new TwoFactorView();
            var window = new WpfDialogWindow(view) { DataContext = vm };
            window.ShowDialog();
        }

        private void ShowDeviceCode(object sender, RoutedEventArgs e)
        {
            var vm = new DeviceCodeViewModel(_environment)
            {
                UserCode = userCode.Text,
                VerificationUrl = verificationUrl.Text,
            };
            var view = new DeviceCodeView();
            var window = new WpfDialogWindow(view) { DataContext = vm };
            window.ShowDialog();
        }
    }
}
