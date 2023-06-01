using System.Threading;
using System.Threading.Tasks;
using GitHub.UI.ViewModels;
using GitHub.UI.Views;
using GitCredentialManager;
using GitCredentialManager.UI;

namespace GitHub.UI.Commands
{
    public class SelectAccountCommandImpl : SelectAccountCommand
    {
        public SelectAccountCommandImpl(ICommandContext context) : base(context) { }

        protected override Task ShowAsync(SelectAccountViewModel viewModel, CancellationToken ct)
        {
            return AvaloniaUi.ShowViewAsync<SelectAccountView>(viewModel, GetParentHandle(), ct);
        }
    }
}
