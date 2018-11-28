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
        private readonly string _header;

        public VersionCommand(string header)
        {
            _header = header;
        }

        public override bool CanExecute(string[] args)
        {
            return args.Any(x => StringComparer.OrdinalIgnoreCase.Equals(x, "--version"))
                || args.Any(x => StringComparer.OrdinalIgnoreCase.Equals(x, "version"));
        }

        public override Task ExecuteAsync(ICommandContext context, string[] args)
        {
            context.StdOut.WriteLine(_header);

            return Task.CompletedTask;
        }
    }
}
