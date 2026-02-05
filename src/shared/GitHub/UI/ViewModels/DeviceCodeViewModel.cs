using System.Windows.Input;
using GitCredentialManager;
using GitCredentialManager.UI;
using GitCredentialManager.UI.ViewModels;

namespace GitHub.UI.ViewModels
{
    public class DeviceCodeViewModel : WindowViewModel
    {
        private readonly ISessionManager _sessionManager;

        private ICommand _verificationUrlCommand;
        private string _verificationUrl;
        private string _userCode;

        public DeviceCodeViewModel()
        {
            // Constructor the XAML designer
        }

        public DeviceCodeViewModel(ISessionManager sessionManager)
        {
            EnsureArgument.NotNull(sessionManager, nameof(sessionManager));

            _sessionManager = sessionManager;

            Title = "Device code authentication";
            VerificationUrlCommand = new RelayCommand(OpenVerificationUrl);
        }

        private void OpenVerificationUrl()
        {
            _sessionManager.OpenBrowser(VerificationUrl);
        }

        public string UserCode
        {
            get => _userCode;
            set => SetAndRaisePropertyChanged(ref _userCode, value);
        }

        public string VerificationUrl
        {
            get => _verificationUrl;
            set => SetAndRaisePropertyChanged(ref _verificationUrl, value);
        }

        public ICommand VerificationUrlCommand
        {
            get => _verificationUrlCommand;
            set => SetAndRaisePropertyChanged(ref _verificationUrlCommand, value);
        }
    }
}
