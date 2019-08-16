// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager.Commands;
using Microsoft.Git.CredentialManager.Interop;

namespace Microsoft.Git.CredentialManager
{
    public class Application : ApplicationBase
    {
        private readonly IHostProviderRegistry _providerRegistry;

        public Application(ICommandContext context)
            : base(context)
        {
            _providerRegistry = new HostProviderRegistry(context);
        }

        public void RegisterProviders(params IHostProvider[] providers)
        {
            _providerRegistry.Register(providers);
        }

        protected override async Task<int> RunInternalAsync(string[] args)
        {
            // Construct all supported commands
            var commands = new CommandBase[]
            {
                new EraseCommand(_providerRegistry),
                new GetCommand(_providerRegistry),
                new StoreCommand(_providerRegistry),
                new VersionCommand(),
                new HelpCommand(),
            };

            // Trace the current version and program arguments
            Context.Trace.WriteLine($"{Constants.GetProgramHeader()} '{string.Join(" ", args)}'");

            if (args.Length == 0)
            {
                Context.Streams.Error.WriteLine("Missing command.");
                HelpCommand.PrintUsage(Context.Streams.Error);
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

            Context.Streams.Error.WriteLine("Unrecognized command '{0}'.", args[0]);
            HelpCommand.PrintUsage(Context.Streams.Error);
            return -1;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _providerRegistry?.Dispose();
            }

            base.Dispose(disposing);
        }

        protected bool WriteException(Exception ex)
        {
            // Try and use a nicer format for some well-known exception types
            switch (ex)
            {
                case InteropException interopEx:
                    Context.Streams.Error.WriteLine("fatal: {0} [0x{1:x}]", interopEx.Message, interopEx.ErrorCode);
                    break;
                default:
                    Context.Streams.Error.WriteLine("fatal: {0}", ex.Message);
                    break;
            }

            // Recurse to print all inner exceptions
            if (!(ex.InnerException is null))
            {
                WriteException(ex.InnerException);
            }

            return true;
        }
    }
}
