// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;

namespace Microsoft.Git.CredentialManager
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            using (var app = new Application(new CommandContext()))
            {
                int exitCode = app.RunAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
               Environment.Exit(exitCode);
            }
        }
    }
}
