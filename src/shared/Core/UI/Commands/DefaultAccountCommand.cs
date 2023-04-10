using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager.UI.ViewModels;

namespace GitCredentialManager.UI.Commands;

public abstract class DefaultAccountCommand : HelperCommand
{
    protected DefaultAccountCommand(ICommandContext context)
        : base(context, "default-account", "Show prompt to confirm use of the default OS account.")
    {
        AddOption(
            new Option<string>("--title", "Window title (optional).")
        );

        AddOption(
            new Option<string>("--username", "User name to display.")
            {
                IsRequired = true
            }
        );

        AddOption(
            new Option("--no-logo", "Hide the Git Credential Manager logo and logotype.")
        );

        Handler = CommandHandler.Create(ExecuteAsync);
    }

    private class CommandOptions
    {
        public string Title { get; set; }
        public string UserName { get; set; }
        public bool NoLogo { get; set; }
    }

    private async Task<int> ExecuteAsync(CommandOptions options)
    {
        var viewModel = new DefaultAccountViewModel(Context.Environment)
        {
            Title = !string.IsNullOrWhiteSpace(options.Title)
                ? options.Title
                : "Git Credential Manager",
            UserName = options.UserName,
            ShowProductHeader = !options.NoLogo
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
