// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;

namespace Microsoft.Git.CredentialManager
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            int exitCode = Application.RunAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
            Environment.Exit(exitCode);
        }
    }
}
