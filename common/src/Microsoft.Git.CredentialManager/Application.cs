// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager.Commands;

namespace Microsoft.Git.CredentialManager
{
    public class Application : ApplicationBase
    {
        public IHostProviderRegistry ProviderRegistry { get; } = new HostProviderRegistry();

        public Application(ICommandContext context)
            : base(context) { }

        protected override async Task<int> RunInternalAsync(string[] args)
        {
            // Construct all supported commands
            var commands = new CommandBase[]
            {
                new EraseCommand(ProviderRegistry),
                new GetCommand(ProviderRegistry),
                new StoreCommand(ProviderRegistry),
                new VersionCommand(),
                new HelpCommand(),
            };

            // Trace the current version and program arguments
            Context.Trace.WriteLine($"{Constants.GetProgramHeader()} '{string.Join(" ", args)}'");

            if (args.Length == 0)
            {
                Context.StdError.WriteLine("Missing command.");
                HelpCommand.PrintUsage(Context.StdError);
                return -1;
            }

            foreach (var cmd in commands)
            {
                if (cmd.CanExecute(args))
                {
                    try
                    {
                        await cmd.ExecuteAsync(Context, args);
                        return 0;
                    }
                    catch (Exception e)
                    {
                        if (e is AggregateException ae)
                        {
                            ae.Handle(WriteException);
                        }
                        else
                        {
                            WriteException(e);
                        }

                        return -1;
                    }
                }
            }

            Context.StdError.WriteLine("Unrecognized command '{0}'.", args[0]);
            HelpCommand.PrintUsage(Context.StdError);
            return -1;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ProviderRegistry.Dispose();
            }

            base.Dispose(disposing);
        }

        protected bool WriteException(Exception e)
        {
            Context.StdError.WriteLine("fatal: {0}", e.Message);
            if (e.InnerException != null)
            {
                Context.StdError.WriteLine("fatal: {0}", e.InnerException.Message);
            }

            return true;
        }
    }
}
