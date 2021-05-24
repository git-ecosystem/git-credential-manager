// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using Atlassian.Bitbucket;
using GitHub;
using Microsoft.AzureRepos;
using Microsoft.Git.CredentialManager.Authentication;

namespace Microsoft.Git.CredentialManager
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            string appPath = ApplicationBase.GetEntryApplicationPath();
            using (var context = new CommandContext(appPath))
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

                // Register all supported host providers at the normal priority.
                // The generic provider should never win against a more specific one, so register it with low priority.
                app.RegisterProvider(new AzureReposHostProvider(context), HostProviderPriority.Normal);
                app.RegisterProvider(new BitbucketHostProvider(context),  HostProviderPriority.Normal);
                app.RegisterProvider(new GitHubHostProvider(context),     HostProviderPriority.Normal);
                app.RegisterProvider(new GenericHostProvider(context),    HostProviderPriority.Low);

                int exitCode = app.RunAsync(args)
                                  .ConfigureAwait(false)
                                  .GetAwaiter()
                                  .GetResult();

                Environment.Exit(exitCode);
            }
        }
    }
}
