using System.Threading;
using System.Threading.Tasks;
using Atlassian.Bitbucket.UI.ViewModels;
using Atlassian.Bitbucket.UI.Views;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.UI;

namespace Atlassian.Bitbucket.UI.Commands
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
