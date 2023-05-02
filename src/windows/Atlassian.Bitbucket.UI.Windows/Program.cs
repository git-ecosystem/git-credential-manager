using System;
using System.Threading.Tasks;
using Atlassian.Bitbucket.UI.Windows.Commands;
using Atlassian.Bitbucket.UI.Windows.Controls;
using GitCredentialManager;
using GitCredentialManager.UI;
using GitCredentialManager.UI.Windows;

namespace Atlassian.Bitbucket.UI.Windows
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            // Set the session id (sid) for the helper process, to be
            // used when TRACE2 tracing is enabled.
            ProcessManager.CreateSid();
            using (var context = new CommandContext())
            using (var app = new HelperApplication(context))
            {
                // Initialize TRACE2 system
                context.Trace2.Initialize(DateTimeOffset.UtcNow);

                // Write the start and version events
                context.Trace2.Start(context.ApplicationPath, args);

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
