using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager;
using GitCredentialManager.UI.Windows;
using GitLab.UI.Commands;
using GitLab.UI.ViewModels;
using GitLab.UI.Windows.Views;

namespace GitLab.UI.Windows.Commands
{
    public class CredentialsCommandImpl : CredentialsCommand
    {
        public CredentialsCommandImpl(ICommandContext context) : base(context) { }

        protected override Task ShowAsync(CredentialsViewModel viewModel, CancellationToken ct)
        {
            return Gui.ShowDialogWindow(viewModel, () => new CredentialsView(), GetParentHandle());
        }
    }
}
