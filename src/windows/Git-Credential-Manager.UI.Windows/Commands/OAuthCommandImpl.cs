using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager.UI.Commands;
using GitCredentialManager.UI.ViewModels;
using GitCredentialManager.UI.Windows.Views;

namespace GitCredentialManager.UI.Windows.Commands
{
    public class OAuthCommandImpl : OAuthCommand
    {
        public OAuthCommandImpl(ICommandContext context) : base(context) { }

        protected override Task ShowAsync(OAuthViewModel viewModel, CancellationToken ct)
        {
            return Gui.ShowDialogWindow(viewModel, () => new OAuthView(), GetParentHandle());
        }
    }
}
