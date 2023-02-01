using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager.UI.ViewModels;
using GitCredentialManager.UI.Views;

namespace GitCredentialManager.UI.Commands
{
    public class DeviceCodeCommandImpl : DeviceCodeCommand
    {
        public DeviceCodeCommandImpl(ICommandContext context) : base(context) { }

        protected override Task ShowAsync(DeviceCodeViewModel viewModel, CancellationToken ct)
        {
            return Gui.ShowDialogWindow(viewModel, () => new DeviceCodeView(), GetParentHandle());
        }
    }
}
