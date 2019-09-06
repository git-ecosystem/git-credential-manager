// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager.Commands
{
    /// <summary>
    /// Print usage information and basic help for Git Credential Manager.
    /// </summary>
    public class HelpCommand : CommandBase
    {
        public override bool CanExecute(string[] args)
        {
            return args.Any(x => StringComparer.OrdinalIgnoreCase.Equals(x, "--help") ||
                                 StringComparer.OrdinalIgnoreCase.Equals(x, "-h") ||
                                 StringComparer.OrdinalIgnoreCase.Equals(x, "help") ||
                                 (x != null && x.Contains('?')));
        }

        public override Task ExecuteAsync(ICommandContext context, string[] args)
        {
            context.Streams.Out.WriteLine(Constants.GetProgramHeader());

            PrintUsage(context.Streams.Out);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Print the standard usage documentation for Git Credential Manager to the given <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="writer">Text writer to write usage information to.</param>
        public static void PrintUsage(TextWriter writer)
        {
            string appName = Path.GetFileName(Assembly.GetEntryAssembly().CodeBase);

            writer.WriteLine();
            writer.WriteLine("usage: {0} <command>", appName);
            writer.WriteLine();
            writer.WriteLine("  Available commands:");
            writer.WriteLine("    erase");
            writer.WriteLine("    get");
            writer.WriteLine("    store");
            writer.WriteLine();
            writer.WriteLine("    configure [--system]");
            writer.WriteLine("    unconfigure [--system]");
            writer.WriteLine();
            writer.WriteLine("    --version, version");
            writer.WriteLine("    --help, -h, -?");
            writer.WriteLine();
        }
    }
}
