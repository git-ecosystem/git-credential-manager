// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using Microsoft.Git.CredentialManager;

namespace Microsoft.Authentication.Helper
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            using (var context = new CommandContext())
            using (var app = new Application(context))
            {
                int exitCode = app.RunAsync(args)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();

                Environment.Exit(exitCode);
            }
        }
    }
}
