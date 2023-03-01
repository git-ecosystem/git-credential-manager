using System.Threading;
using System.Threading.Tasks;
using GitHub.UI.Commands;
using GitHub.UI.ViewModels;
using GitHub.UI.Windows.Views;
using GitCredentialManager;
using GitCredentialManager.UI.Windows;

namespace GitHub.UI.Windows.Commands
{
    public class SelectAccountCommandImpl : SelectAccountCommand
    {
        public SelectAccountCommandImpl(ICommandContext context) : base(context) { }

        protected override Task ShowAsync(SelectAccountViewModel viewModel, CancellationToken ct)
        {
            return Gui.ShowDialogWindow(viewModel, () => new SelectAccountView(), GetParentHandle());
        }
    }
}
