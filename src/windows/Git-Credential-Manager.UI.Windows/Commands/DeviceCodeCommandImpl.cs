using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager.UI.Commands;
using GitCredentialManager.UI.ViewModels;
using GitCredentialManager.UI.Windows.Views;

namespace GitCredentialManager.UI.Windows.Commands
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
