using System.Threading;
using System.Threading.Tasks;
using Gitee.UI.ViewModels;
using Gitee.UI.Views;
using GitCredentialManager;
using GitCredentialManager.UI;

namespace Gitee.UI.Commands
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
