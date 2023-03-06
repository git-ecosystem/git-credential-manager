using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Atlassian.Bitbucket.UI.Commands;
using Atlassian.Bitbucket.UI.Controls;
using GitCredentialManager;
using GitCredentialManager.UI;

namespace Atlassian.Bitbucket.UI
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            // Set the session id (sid) for the helper process, to be
            // used when TRACE2 tracing is enabled.
            SidManager.CreateSid();
            using (var context = new CommandContext(args))
            using (var app = new HelperApplication(context))
            {
                if (args.Length == 0)
                {
                    await Gui.ShowWindow(() => new TesterWindow(), IntPtr.Zero);
                    return;
                }

                app.RegisterCommand(new CredentialsCommandImpl(context));

                int exitCode = app.RunAsync(args)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();

                context.Trace2.Stop(exitCode);
                Environment.Exit(exitCode);
            }
        }
    }
}
