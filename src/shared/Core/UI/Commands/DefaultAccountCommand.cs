using System.Collections.Generic;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager.UI.ViewModels;

namespace GitCredentialManager.UI.Commands;

public abstract class DefaultAccountCommand : HelperCommand
{
    protected DefaultAccountCommand(ICommandContext context)
        : base(context, "default-account", "Show prompt to confirm use of the default OS account.")
    {
        var title = new Option<string>("--title", "Window title (optional).");
        AddOption(title);

        var userName = new Option<string>("--username", "User name to display.")
        {
            IsRequired = true
        };
        AddOption(userName);

        var noLogo = new Option<bool>("--no-logo", "Hide the Git Credential Manager logo and logotype.");
        AddOption(noLogo);

        this.SetHandler(ExecuteAsync, title, userName, noLogo);
    }

    private async Task<int> ExecuteAsync(string title, string userName, bool noLogo)
    {
        var viewModel = new DefaultAccountViewModel(Context.Environment)
        {
            Title = !string.IsNullOrWhiteSpace(title)
                ? title
                : "Git Credential Manager",
            UserName = userName,
            ShowProductHeader = !noLogo
        };

        await ShowAsync(viewModel, CancellationToken.None);

        if (!viewModel.WindowResult)
        {
            throw new Trace2Exception(Context.Trace2, "User cancelled dialog.");
        }

        WriteResult(
            new Dictionary<string, string>
            {
                ["use_default_account"] = viewModel.UseDefaultAccount ? "1" : "0"
            }
        );

        return 0;
    }

    protected abstract Task ShowAsync(DefaultAccountViewModel viewModel, CancellationToken ct);
}
