using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager.UI.Commands;
using GitCredentialManager.UI.ViewModels;
using GitCredentialManager.UI.Windows.Views;

namespace GitCredentialManager.UI.Windows.Commands
{
    public class DefaultAccountCommandImpl : DefaultAccountCommand
    {
        public DefaultAccountCommandImpl(ICommandContext context) : base(context) { }

        protected override Task ShowAsync(DefaultAccountViewModel viewModel, CancellationToken ct)
        {
            return Gui.ShowDialogWindow(viewModel, () => new DefaultAccountView(), GetParentHandle());
        }
    }
}
