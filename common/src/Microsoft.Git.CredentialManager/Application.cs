// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager.Commands;

namespace Microsoft.Git.CredentialManager
{
    public class Application : IDisposable
    {
        private readonly ICommandContext _context;

        private TextWriter _traceFileWriter;

        public IHostProviderRegistry ProviderRegistry { get; } = new HostProviderRegistry();

        public Application(ICommandContext context)
        {
            EnsureArgument.NotNull(context, nameof(context));

            _context = context;
        }

        public async Task<int> RunAsync(string[] args)
        {
            // Launch debugger
            if (_context.IsEnvironmentVariableTruthy(Constants.EnvironmentVariables.GcmDebug, false))
            {
                _context.StdError.WriteLine("Waiting for debugger to be attached...");
                PlatformUtils.WaitForDebuggerAttached();

                // Now the debugger is attached, break!
                Debugger.Break();
            }

            // Enable tracing
            if (_context.TryGetEnvironmentVariable(Constants.EnvironmentVariables.GcmTrace, out string traceEnvar))
            {
                if (traceEnvar.IsTruthy()) // Trace to stderr
                {
                    _context.Trace.AddListener(_context.StdError);
                }
                else if (Path.IsPathRooted(traceEnvar) && // Trace to a file
                         TryCreateTextWriter(_context, traceEnvar, out var fileWriter))
                {
                    _traceFileWriter = fileWriter;
                    _context.Trace.AddListener(fileWriter);
                }
                else
                {
                    _context.StdError.WriteLine($"warning: cannot write trace output to {traceEnvar}");
                }
            }

            // Enable sensitive tracing and show warning
            if (_context.IsEnvironmentVariableTruthy(Constants.EnvironmentVariables.GcmTraceSecrets, false))
            {
                _context.Trace.EnableSecretTracing = true;
                _context.StdError.WriteLine("Secret tracing is enabled. Trace output may contain sensitive information.");
            }

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
            _context.Trace.WriteLine($"{Constants.GetProgramHeader()} '{string.Join(" ", args)}'");

            if (args.Length == 0)
            {
                _context.StdError.WriteLine("Missing command.");
                HelpCommand.PrintUsage(_context.StdError);
                return -1;
            }

            foreach (var cmd in commands)
            {
                if (cmd.CanExecute(args))
                {
                    try
                    {
                        await cmd.ExecuteAsync(_context, args);
                        return 0;
                    }
                    catch (Exception e)
                    {
                        if (e is AggregateException ae)
                        {
                            ae.Handle(x => WriteException(_context, x));
                        }
                        else
                        {
                            WriteException(_context, e);
                        }

                        return -1;
                    }
                }
            }

            _context.StdError.WriteLine("Unrecognized command '{0}'.", args[0]);
            HelpCommand.PrintUsage(_context.StdError);
            return -1;
        }

        #region Helpers

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

        #endregion

        #region IDisposable

        public void Dispose()
        {
            ProviderRegistry.Dispose();
            _traceFileWriter?.Dispose();
        }

        #endregion
    }
}
