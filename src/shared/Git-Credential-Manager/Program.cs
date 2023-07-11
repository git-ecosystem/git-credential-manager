using System;
using System.Threading;
using Atlassian.Bitbucket;
using Avalonia;
using GitHub;
using GitLab;
using Microsoft.AzureRepos;
using GitCredentialManager.Authentication;
using GitCredentialManager.UI;

namespace GitCredentialManager
{
    public static class Program
    {
        private static int _exitCode;

        public static void Main(string[] args)
        {
            // Create the dispatcher on the main thread. This is required
            // for some platform UI services such as macOS that mandates
            // all controls are created/accessed on the initial thread
            // created by the process (the process entry thread).
            Dispatcher.Initialize();

            // Run AppMain in a new thread and keep the main thread free
            // to process the dispatcher's job queue.
            var appMain = new Thread(AppMain) {Name = nameof(AppMain)};
            appMain.Start(args);

            // Process the dispatcher job queue (aka: message pump, run-loop, etc...)
            // We must ensure to run this on the same thread that it was created on
            // (the main thread) so we cannot use any async/await calls between
            // Dispatcher.Initialize and Run.
            Dispatcher.MainThread.Run();

            // Dispatcher was shutdown
            Environment.Exit(_exitCode);
        }

        private static void AppMain(object o)
        {
            string[] args = (string[])o;

            var startTime = DateTimeOffset.UtcNow;
            // Set the session id (sid) and start time for the GCM process, to be
            // used when TRACE2 tracing is enabled.
            ProcessManager.CreateSid();

            using (var context = new CommandContext())
            using (var app = new Application(context))
            {
                // Initialize TRACE2 system
                context.Trace2.Initialize(startTime);

                // Write the start and version events
                context.Trace2.Start(context.ApplicationPath, args);

                // Register all supported host providers at the normal priority.
                // The generic provider should never win against a more specific one, so register it with low priority.
                app.RegisterProvider(new AzureReposHostProvider(context), HostProviderPriority.Normal);
                app.RegisterProvider(new BitbucketHostProvider(context), HostProviderPriority.Normal);
                app.RegisterProvider(new GitHubHostProvider(context), HostProviderPriority.Normal);
                app.RegisterProvider(new GitLabHostProvider(context), HostProviderPriority.Normal);
                app.RegisterProvider(new GenericHostProvider(context), HostProviderPriority.Low);

                _exitCode = app.RunAsync(args)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();

                context.Trace2.Stop(_exitCode);
                Dispatcher.MainThread.Shutdown();
            }
        }

        // Required for Avalonia designer
        static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<AvaloniaApp>()
#if NETFRAMEWORK
                .UseWin32()
                .UseSkia()
#else
                .UsePlatformDetect()
#endif
                .LogToTrace();
    }
}
