// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using GitHub;
using Microsoft.AzureRepos;

namespace Microsoft.Git.CredentialManager
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var context = new CommandContext();
            using (var app = new Application(context))
            {
                // Register all supported host providers
                app.ProviderRegistry.Register(
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
    }
}
