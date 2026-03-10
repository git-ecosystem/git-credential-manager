using System.Windows.Input;
using GitCredentialManager.Authentication;

namespace GitCredentialManager.UI.ViewModels;

public class EnableNtlmViewModel : WindowViewModel
{
    private readonly ISessionManager _sessionManager;

    private string _url;
    private ICommand _onceCommand;
    private ICommand _alwaysCommand;
    private ICommand _noCommand;
    private ICommand _learnMoreCommand;

    public EnableNtlmViewModel()
    {
        // For designer only
    }
    
    public EnableNtlmViewModel(ISessionManager sessionManager) : this()
    {
        _sessionManager = sessionManager;

        OnceCommand = new RelayCommand(() => SelectOption(NtlmSupport.Once));
        AlwaysCommand = new RelayCommand(() => SelectOption(NtlmSupport.Always));
        NoCommand = new RelayCommand(() => SelectOption(NtlmSupport.Disabled));
        LearnMoreCommand = new RelayCommand(() => sessionManager.OpenBrowser(Constants.HelpUrls.GcmNtlm));
    }

    private void SelectOption(NtlmSupport option)
    {
        SelectedOption = option;
        Accept();
    }

    public NtlmSupport SelectedOption { get; private set; }

    public string Url
    {
        get => _url;
        set => SetAndRaisePropertyChanged(ref _url, value);
    }

    public ICommand OnceCommand
    {
        get => _onceCommand;
        set => SetAndRaisePropertyChanged(ref _onceCommand, value);
    }

    public ICommand AlwaysCommand
    {
        get => _alwaysCommand;
        set => SetAndRaisePropertyChanged(ref _alwaysCommand, value);
    }

    public ICommand NoCommand
    {
        get => _noCommand;
        set => SetAndRaisePropertyChanged(ref _noCommand, value);
    }

    public ICommand LearnMoreCommand
    {
        get => _learnMoreCommand;
        set => SetAndRaisePropertyChanged(ref _learnMoreCommand, value);
    }
}
