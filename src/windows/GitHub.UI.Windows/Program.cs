using System;
using System.Threading.Tasks;
using GitCredentialManager;
using GitCredentialManager.UI;
using GitCredentialManager.UI.Windows;
using GitHub.UI.Windows.Commands;
using GitHub.UI.Windows.Controls;

namespace GitHub.UI.Windows
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
                app.RegisterCommand(new TwoFactorCommandImpl(context));
                app.RegisterCommand(new DeviceCodeCommandImpl(context));
                app.RegisterCommand(new SelectAccountCommandImpl(context));

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
