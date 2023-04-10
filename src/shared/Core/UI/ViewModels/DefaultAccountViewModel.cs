using System.Windows.Input;

namespace GitCredentialManager.UI.ViewModels;

public class DefaultAccountViewModel : WindowViewModel
{
    private readonly IEnvironment _env;

    private bool _showProductHeader = true;
    private string _userName;
    private ICommand _continueCommand;
    private ICommand _otherAccountCommand;
    private ICommand _learnMoreCommand;

    public DefaultAccountViewModel()
    {
        // For designer only
    }
    
    public DefaultAccountViewModel(IEnvironment environment) : this()
    {
        _env = environment;

        ContinueCommand = new RelayCommand(Continue);
        OtherAccountCommand = new RelayCommand(OtherAccount);
        LearnMoreCommand = new RelayCommand(OpenLink);
    }

    private void OtherAccount()
    {
        UseDefaultAccount = false;
        Accept();
    }

    private void Continue()
    {
        UseDefaultAccount = true;
        Accept();
    }

    private void OpenLink()
    {
        BrowserUtils.OpenDefaultBrowser(_env, Link);
    }

    public bool UseDefaultAccount { get; private set; }

    public string Link => Constants.HelpUrls.GcmDefaultAccount;

    public bool ShowProductHeader
    {
        get => _showProductHeader;
        set => SetAndRaisePropertyChanged(ref _showProductHeader, value);
    }

    public string UserName
    {
        get => _userName;
        set => SetAndRaisePropertyChanged(ref _userName, value);
    }

    public ICommand ContinueCommand
    {
        get => _continueCommand;
        set => SetAndRaisePropertyChanged(ref _continueCommand, value);
    }

    public ICommand OtherAccountCommand
    {
        get => _otherAccountCommand;
        set => SetAndRaisePropertyChanged(ref _otherAccountCommand, value);
    }

    public ICommand LearnMoreCommand
    {
        get => _learnMoreCommand;
        set => SetAndRaisePropertyChanged(ref _learnMoreCommand, value);
    }
}
