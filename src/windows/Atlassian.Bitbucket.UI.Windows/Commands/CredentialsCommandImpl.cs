using System.Threading;
using System.Threading.Tasks;
using Atlassian.Bitbucket.UI.Commands;
using Atlassian.Bitbucket.UI.ViewModels;
using Atlassian.Bitbucket.UI.Windows.Views;
using GitCredentialManager;
using GitCredentialManager.UI.Windows;

namespace Atlassian.Bitbucket.UI.Windows.Commands
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
