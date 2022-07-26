using System.ComponentModel;

namespace GitCredentialManager.UI.ViewModels
{
    public class CredentialsViewModel : WindowViewModel
    {
        private string _userName;
        private string _password;
        private string _description;
        private bool _showProductHeader;
        private RelayCommand _signInCommand;

        public CredentialsViewModel()
        {
            SignInCommand = new RelayCommand(Accept, CanSignIn);
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(UserName):
                case nameof(Password):
                    SignInCommand.RaiseCanExecuteChanged();
                    break;
            }
        }

        private bool CanSignIn()
        {
            // Allow empty username or empty password, or both!
            // This is what the older Windows API CredUIPromptForWindowsCredentials
            // permitted so we should continue to support any possible scenarios.
            return true;
        }

        public string UserName
        {
            get => _userName;
            set => SetAndRaisePropertyChanged(ref _userName, value);
        }

        public string Password
        {
            get => _password;
            set => SetAndRaisePropertyChanged(ref _password, value);
        }

        public string Description
        {
            get => _description;
            set => SetAndRaisePropertyChanged(ref _description, value);
        }

        public bool ShowProductHeader
        {
            get => _showProductHeader;
            set => _showProductHeader = value;
        }

        public RelayCommand SignInCommand
        {
            get => _signInCommand;
            set => SetAndRaisePropertyChanged(ref _signInCommand, value);
        }
    }
}
