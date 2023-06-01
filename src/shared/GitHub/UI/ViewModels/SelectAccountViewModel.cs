using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using GitCredentialManager;
using GitCredentialManager.UI;
using GitCredentialManager.UI.ViewModels;

namespace GitHub.UI.ViewModels
{
    public class SelectAccountViewModel : WindowViewModel
    {
        private readonly IEnvironment _environment;

        private AccountViewModel _selectedAccount;
        private string _enterpriseUrl;
        private ObservableCollection<AccountViewModel> _accounts;
        private RelayCommand _continueCommand;
        private ICommand _newAccountCommand;
        private ICommand _learnMoreCommand;
        private bool _showHelpLink = true;

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SelectedAccount):
                    ContinueCommand.RaiseCanExecuteChanged();
                    break;
            }
        }

        public SelectAccountViewModel()
        {
            // Constructor the XAML designer
        }

        public SelectAccountViewModel(IEnvironment environment, IEnumerable<string> accounts = null)
        {
            EnsureArgument.NotNull(environment, nameof(environment));

            _environment = environment;

            Title = "Select an account";
            ContinueCommand = new RelayCommand(Accept, CanContinue);
            NewAccountCommand = new RelayCommand(NewAccount);
            LearnMoreCommand = new RelayCommand(LearnMore);
            Accounts = new ObservableCollection<AccountViewModel>();

            foreach (string account in accounts ?? Enumerable.Empty<string>())
            {
                Accounts.Add(
                    new AccountViewModel
                    {
                        UserName = account
                    }
                );
            }

            PropertyChanged += OnPropertyChanged;
        }

        private void NewAccount()
        {
            SelectedAccount = null;
            Accept();
        }

        private void LearnMore()
        {
            BrowserUtils.OpenDefaultBrowser(_environment, Constants.HelpUrls.GcmMultipleUsers);
        }

        private bool CanContinue()
        {
            return SelectedAccount != null;
        }

        public AccountViewModel SelectedAccount
        {
            get => _selectedAccount;
            set => SetAndRaisePropertyChanged(ref _selectedAccount, value);
        }

        public string EnterpriseUrl
        {
            get => _enterpriseUrl;
            set => SetAndRaisePropertyChanged(ref _enterpriseUrl, value);
        }

        public ObservableCollection<AccountViewModel> Accounts
        {
            get => _accounts;
            set => SetAndRaisePropertyChanged(ref _accounts, value);
        }

        public RelayCommand ContinueCommand
        {
            get => _continueCommand;
            set => SetAndRaisePropertyChanged(ref _continueCommand, value);
        }

        public ICommand NewAccountCommand
        {
            get => _newAccountCommand;
            set => SetAndRaisePropertyChanged(ref _newAccountCommand, value);
        }

        public ICommand LearnMoreCommand
        {
            get => _learnMoreCommand;
            set => SetAndRaisePropertyChanged(ref _learnMoreCommand, value);
        }

        public bool ShowHelpLink
        {
            get => _showHelpLink;
            set => SetAndRaisePropertyChanged(ref _showHelpLink, value);
        }
    }

    public class AccountViewModel : ViewModel
    {
        private string _userName;

        public string UserName
        {
            get => _userName;
            set => SetAndRaisePropertyChanged(ref _userName, value);
        }
    }
}
