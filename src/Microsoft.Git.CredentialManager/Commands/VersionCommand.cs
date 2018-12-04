// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager.Commands
{
    /// <summary>
    /// Print version information for Git Credential Manager.
    /// </summary>
    public class VersionCommand : CommandBase
    {
        public override bool CanExecute(string[] args)
        {
            return args.Any(x => StringComparer.OrdinalIgnoreCase.Equals(x, "--version"))
                || args.Any(x => StringComparer.OrdinalIgnoreCase.Equals(x, "version"));
        }

        public override Task ExecuteAsync(ICommandContext context, string[] args)
        {
            string appHeader = Constants.GetProgramHeader();

            context.StdOut.WriteLine(appHeader);

            return Task.CompletedTask;
        }
    }
}
