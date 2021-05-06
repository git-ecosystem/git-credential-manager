// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Atlassian.Bitbucket;
using GitHub;
using Microsoft.AzureRepos;

namespace Microsoft.Git.CredentialManager
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            string appPath = GetApplicationPath();
            using (var context = new CommandContext(appPath))
            using (var app = new Application(context))
            {
                // Register all supported host providers at the normal priority.
                // The generic provider should never win against a more specific one, so register it with low priority.
                app.RegisterProvider(new AzureReposHostProvider(context), HostProviderPriority.Normal);
                app.RegisterProvider(new BitbucketHostProvider(context),  HostProviderPriority.Normal);
                app.RegisterProvider(new GitHubHostProvider(context),     HostProviderPriority.Normal);
                app.RegisterProvider(new GenericHostProvider(context),    HostProviderPriority.Low);

                // Run!
                int exitCode = app.RunAsync(args)
                                  .ConfigureAwait(false)
                                  .GetAwaiter()
                                  .GetResult();

                Environment.Exit(exitCode);
            }
        }

        private static string GetApplicationPath()
        {
            // Assembly::Location always returns an empty string if the application was published as a single file
#pragma warning disable IL3000
            bool isSingleFile = string.IsNullOrEmpty(Assembly.GetEntryAssembly()?.Location);
#pragma warning restore IL3000

            // Use "argv[0]" to get the full path to the entry executable - this is consistent across
            // .NET Framework and .NET >= 5 when published as a single file.
            string[] args = Environment.GetCommandLineArgs();
            string candidatePath = args[0];

            // If we have not been published as a single file on .NET 5 then we must strip the ".dll" file extension
            // to get the default AppHost/SuperHost name.
            if (!isSingleFile && Path.HasExtension(candidatePath))
            {
                return Path.ChangeExtension(candidatePath, null);
            }

            return candidatePath;
        }
    }
}
