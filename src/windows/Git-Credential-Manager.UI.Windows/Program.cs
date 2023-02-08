using System;
using System.Diagnostics;
using System.Threading.Tasks;
using GitCredentialManager.UI.Commands;
using GitCredentialManager.UI.Controls;

namespace GitCredentialManager.UI
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            using (var context = new CommandContext(args))
            using (var app = new HelperApplication(context))
            {
                if (args.Length == 0)
                {
                    await Gui.ShowWindow(() => new TesterWindow(), IntPtr.Zero);
                    return;
                }

                app.RegisterCommand(new CredentialsCommandImpl(context));
                app.RegisterCommand(new OAuthCommandImpl(context));
                app.RegisterCommand(new DeviceCodeCommandImpl(context));

                int exitCode = app.RunAsync(args)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();

                Environment.Exit(exitCode);
            }
        }
    }
}
