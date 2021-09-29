using System;
using System.Threading.Tasks;
using Atlassian.Bitbucket.UI.Commands;
using Atlassian.Bitbucket.UI.Controls;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.UI;

namespace Atlassian.Bitbucket.UI
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            string appPath = ApplicationBase.GetEntryApplicationPath();
            using (var context = new CommandContext(appPath))
            using (var app = new HelperApplication(context))
            {
                if (args.Length == 0)
                {
                    await Gui.ShowWindow(() => new TesterWindow(), IntPtr.Zero);
                    return;
                }

                app.RegisterCommand(new CredentialsCommandImpl(context));
                app.RegisterCommand(new OAuthCommandImpl(context));

                int exitCode = app.RunAsync(args)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();

                Environment.Exit(exitCode);
            }
        }
    }
}
