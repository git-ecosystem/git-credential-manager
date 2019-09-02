// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.IO;
using System.Reflection;
using GitHub;
using Microsoft.AzureRepos;

namespace Microsoft.Git.CredentialManager
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            string appPath = GetApplicationPath();
            using (var context = new CommandContext())
            using (var app = new Application(context, appPath))
            {
                // Register all supported host providers
                app.RegisterProviders(
                    new AzureReposHostProvider(context),
                    new GitHubHostProvider(context),
                    new GenericHostProvider(context)
                );

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
            Assembly entryAssembly = Assembly.GetExecutingAssembly();
            if (entryAssembly is null)
            {
                throw new InvalidOperationException();
            }

            string candidatePath = entryAssembly.Location;

            // Strip the .dll from assembly name on Mac and Linux
            if (!PlatformUtils.IsWindows() && Path.HasExtension(candidatePath))
            {
                return Path.ChangeExtension(candidatePath, null);
            }

            return candidatePath;
        }
    }
}
