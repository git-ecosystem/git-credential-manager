using System;
using Atlassian.Bitbucket;
using GitHub;
using GitLab;
using Microsoft.AzureRepos;
using GitCredentialManager.Authentication;

namespace GitCredentialManager
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            using (var context = new CommandContext(args))
            using (var app = new Application(context))
            {
                // Workaround for https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2560
                if (MicrosoftAuthentication.CanUseBroker(context))
                {
                    try
                    {
                        MicrosoftAuthentication.InitializeBroker();
                    }
                    catch (Exception ex)
                    {
                        context.Streams.Error.WriteLine(
                            "warning: broker initialization failed{0}{1}",
                            Environment.NewLine, ex.Message
                        );
                    }
                }
                
                //
                // Git Credential Manager's executable used to be named "git-credential-manager-core" before
                // dropping the "-core" suffix. In order to prevent "helper not found" errors for users who
                // haven't updated their configuration, we include either a 'shim' or symlink with the old name
                // that print warning messages about using the old name, and then continue execution of GCM.
                //
                // On Windows the shim is an exact copy of the main "git-credential-manager.exe" executable
                // with the old name. We inspect argv[0] to see which executable we are launched as.
                //
                // On UNIX systems we do the same check, except instead of a copy we use a symlink.
                //
                if (!string.IsNullOrWhiteSpace(context.ApplicationPath))
                {
                    // Trim any (.exe) file extension if we're on Windows
                    // Note that in some circumstances (like being called by Git when config is set
                    // to just `helper = manager-core`) we don't always have ".exe" at the end.
                    if (PlatformUtils.IsWindows() && context.ApplicationPath.EndsWith(".exe",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        context.ApplicationPath = context.ApplicationPath
                            .Substring(0, context.ApplicationPath.Length - 4);
                    }
                    if (context.ApplicationPath.EndsWith("git-credential-manager-core",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        context.Streams.Error.WriteLine(
                            "warning: git-credential-manager-core was renamed to git-credential-manager");
                        context.Streams.Error.WriteLine(
                            $"warning: see {Constants.HelpUrls.GcmExecRename} for more information");
                    }
                }

                // Register all supported host providers at the normal priority.
                // The generic provider should never win against a more specific one, so register it with low priority.
                app.RegisterProvider(new AzureReposHostProvider(context), HostProviderPriority.Normal);
                app.RegisterProvider(new BitbucketHostProvider(context),  HostProviderPriority.Normal);
                app.RegisterProvider(new GitHubHostProvider(context),     HostProviderPriority.Normal);
                app.RegisterProvider(new GitLabHostProvider(context),     HostProviderPriority.Normal);
                app.RegisterProvider(new GenericHostProvider(context),    HostProviderPriority.Low);

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
