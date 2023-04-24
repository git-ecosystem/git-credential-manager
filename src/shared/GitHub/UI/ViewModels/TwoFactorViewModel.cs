using System.ComponentModel;
using System.Windows.Input;
using GitCredentialManager;
using GitCredentialManager.UI;
using GitCredentialManager.UI.ViewModels;

namespace GitHub.UI.ViewModels
{
    public class TwoFactorViewModel : WindowViewModel
    {
        private readonly IEnvironment _environment;
        private readonly IProcessManager _processManager;

        private string _code;
        private ICommand _learnMoreCommand;
        private RelayCommand _verifyCommand;
        private bool _isSms;

        public TwoFactorViewModel()
        {
            // Constructor the XAML designer
        }

        public TwoFactorViewModel(IEnvironment environment, IProcessManager processManager)
        {
            EnsureArgument.NotNull(environment, nameof(environment));
            EnsureArgument.NotNull(processManager, nameof(processManager));

            _environment = environment;
            _processManager = processManager;

            Title = "Two-factor authentication required";
            LearnMoreCommand = new RelayCommand(LearnMore);
            VerifyCommand = new RelayCommand(Accept, CanVerify);

            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Code):
                    VerifyCommand.RaiseCanExecuteChanged();
                    break;
            }
        }

        private void LearnMore()
        {
            BrowserUtils.OpenDefaultBrowser(_environment, "https://aka.ms/vs-core-github-auth-help");
        }
        private bool CanVerify()
        {
            return !string.IsNullOrWhiteSpace(Code) && Code.Length == 6;
        }

        public bool IsSms
        {
            get => _isSms;
            set
            {
                SetAndRaisePropertyChanged(ref _isSms, value);
                RaisePropertyChanged(nameof(Description));
            }
        }

        public string Description => IsSms
            ? "We sent you a message via SMS with your authentication code."
            : "Open the two-factor authentication app on your device to view your authentication code.";

        public string Code
        {
            get => _code;
            set => SetAndRaisePropertyChanged(ref _code, value);
        }

        public ICommand LearnMoreCommand
        {
            get => _learnMoreCommand;
            set => SetAndRaisePropertyChanged(ref _learnMoreCommand, value);
        }

        public RelayCommand VerifyCommand
        {
            get => _verifyCommand;
            set => SetAndRaisePropertyChanged(ref _verifyCommand, value);
        }
    }
}
