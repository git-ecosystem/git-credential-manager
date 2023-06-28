using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager;
using GitCredentialManager.UI;
using GitCredentialManager.UI.Windows;
using GitHub.UI.Commands;
using GitHub.UI.ViewModels;
using GitHub.UI.Windows.Views;

namespace GitHub.UI.Windows.Commands
{
    public class TwoFactorCommandImpl : TwoFactorCommand
    {
        public TwoFactorCommandImpl(ICommandContext context) : base(context) { }

        protected override Task ShowAsync(TwoFactorViewModel viewModel, CancellationToken ct)
        {
            return Gui.ShowDialogWindow(viewModel, () => new TwoFactorView(), GetParentHandle());
        }
    }
}
