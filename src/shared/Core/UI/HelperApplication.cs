using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

namespace GitCredentialManager.UI
{
    public class HelperApplication : ApplicationBase
    {
        private readonly IList<Command> _commands = new List<Command>();

        public HelperApplication(ICommandContext context) : base(context)
        {
        }

        protected override async Task<int> RunInternalAsync(string[] args)
        {
            var rootCommand = new RootCommand();

            foreach (Command command in _commands)
            {
                rootCommand.AddCommand(command);
            }

            var parser = new CommandLineBuilder(rootCommand)
                .UseDefaults()
                .UseExceptionHandler(OnException)
                .Build();

            return await parser.InvokeAsync(args);
        }

        public void RegisterCommand(Command command)
        {
            _commands.Add(command);
        }

        private void OnException(Exception ex, InvocationContext invocationContext)
        {
            if (ex is AggregateException aex)
            {
                aex.Handle(WriteException);
            }
            else
            {
                WriteException(ex);
            }

            invocationContext.ExitCode = -1;
        }

        private bool WriteException(Exception ex)
        {
            Context.Streams.Out.WriteDictionary(new Dictionary<string, string>
            {
                ["error"] = ex.Message
            });

            return true;
        }
    }
}
