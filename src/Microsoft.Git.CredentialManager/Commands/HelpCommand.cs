using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager.Commands
{
    /// <summary>
    /// Print usage information and basic help for Git Credential Manager.
    /// </summary>
    public class HelpCommand : CommandBase
    {
        private readonly string _appName;

        public HelpCommand(string appName)
        {
            _appName = appName ?? throw new ArgumentNullException(nameof(appName));
        }

        public override bool CanExecute(string[] args)
        {
            return args.Any(x => StringComparer.OrdinalIgnoreCase.Equals(x, "--help") ||
                                 StringComparer.OrdinalIgnoreCase.Equals(x, "-h") ||
                                 StringComparer.OrdinalIgnoreCase.Equals(x, "help") ||
                                 (x != null && x.Contains('?')));
        }

        public override Task ExecuteAsync(ICommandContext context, string[] args)
        {
            context.StdOut.WriteLine(Constants.GcmProgramNameFormat, Constants.GcmVersion, PlatformUtils.GetOSInfo());

            PrintUsage(context.StdOut);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Print the standard usage documentation for Git Credential Manager to the given <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="writer">Text writer to write usage information to.</param>
        public void PrintUsage(TextWriter writer)
        {
            writer.WriteLine();
            writer.WriteLine("usage: {0} <command>", _appName);
            writer.WriteLine();
            writer.WriteLine("  Available commands:");
            writer.WriteLine("    erase");
            writer.WriteLine("    get");
            writer.WriteLine("    store");
            writer.WriteLine();
            writer.WriteLine("    --version, version");
            writer.WriteLine("    --help, -h, -?");
            writer.WriteLine();
        }
    }
}
