// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AzureRepos;
using Microsoft.Git.CredentialManager.Commands;

namespace Microsoft.Git.CredentialManager
{
    public static class Application
    {
        private static readonly IHostProviderRegistry HostProviderRegistry = new HostProviderRegistry();

        private static readonly ICollection<CommandBase> Commands = new CommandBase[]
        {
            new EraseCommand(HostProviderRegistry),
            new GetCommand(HostProviderRegistry),
            new StoreCommand(HostProviderRegistry),
            new VersionCommand(),
            new HelpCommand(),
        };

        public static async Task<int> RunAsync(string[] args)
        {
            var context = new CommandContext();

            // Launch debugger
            if (context.IsEnvironmentVariableTruthy(Constants.EnvironmentVariables.GcmDebug, false))
            {
                context.StdError.WriteLine("Waiting for debugger to be attached...");
                PlatformUtils.WaitForDebuggerAttached();

                // Now the debugger is attached, break!
                Debugger.Break();
            }

            // Enable tracing
            if (context.TryGetEnvironmentVariable(Constants.EnvironmentVariables.GcmTrace, out string traceEnvar))
            {
                if (traceEnvar.IsTruthy()) // Trace to stderr
                {
                    context.Trace.AddListener(context.StdError);
                }
                else if (Path.IsPathRooted(traceEnvar) && // Trace to a file
                         TryCreateTextWriter(context, traceEnvar, out var fileWriter))
                {
                    context.Trace.AddListener(fileWriter);
                }
                else
                {
                    context.StdError.WriteLine($"warning: cannot write trace output to {traceEnvar}");
                }
            }

            // Enable sensitive tracing and show warning
            if (context.IsEnvironmentVariableTruthy(Constants.EnvironmentVariables.GcmTraceSecrets, false))
            {
                context.Trace.EnableSecretTracing = true;
                context.StdError.WriteLine("Secret tracing is enabled. Trace output may contain sensitive information.");
            }

            // Register all supported host providers
            HostProviderRegistry.Register(
                new AzureReposHostProvider(context),
                new GenericHostProvider(context)
            );

            // Trace the current version and program arguments
            context.Trace.WriteLine($"{Constants.GetProgramHeader()} '{string.Join(" ", args)}'");

            if (args.Length == 0)
            {
                context.StdError.WriteLine("Missing command.");
                HelpCommand.PrintUsage(context.StdError);
                return -1;
            }

            foreach (var cmd in Commands)
            {
                if (cmd.CanExecute(args))
                {
                    try
                    {
                        await cmd.ExecuteAsync(context, args);
                        return 0;
                    }
                    catch (Exception e)
                    {
                        if (e is AggregateException ae)
                        {
                            ae.Handle(x => WriteException(context, x));
                        }
                        else
                        {
                            WriteException(context, e);
                        }

                        return -1;
                    }
                }
            }

            context.StdError.WriteLine("Unrecognized command '{0}'.", args[0]);
            HelpCommand.PrintUsage(context.StdError);
            return -1;
        }

        private static bool WriteException(ICommandContext context, Exception e)
        {
            context.StdError.WriteLine("fatal: {0}", e.Message);
            if (e.InnerException != null)
            {
                context.StdError.WriteLine("fatal: {0}", e.InnerException.Message);
            }

            return true;
        }

        private static bool TryCreateTextWriter(ICommandContext context, string path, out TextWriter writer)
        {
            writer = null;

            try
            {
                var utf8NoBomEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

                var stream = context.FileSystem.OpenFileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                writer = new StreamWriter(stream, utf8NoBomEncoding, 4096, leaveOpen: false);
            }
            catch
            {
                // Swallow all exceptions
            }

            return writer != null;
        }
    }
}
