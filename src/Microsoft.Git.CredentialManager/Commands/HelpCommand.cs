using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager.Commands
{
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
                                 x != null && x.Contains('?'));
        }

        public override Task ExecuteAsync(ICommandContext context, string[] args)
        {
            context.StdOut.WriteLine(Constants.GcmProgramNameFormat, Constants.GcmVersion, PlatformUtils.GetOSInfo());
            context.StdOut.WriteLine();
            context.StdOut.WriteLine("usage: {0} <command>", _appName);
            context.StdOut.WriteLine();
            context.StdOut.WriteLine("  Available commands:");
            context.StdOut.WriteLine("    erase");
            context.StdOut.WriteLine("    get");
            context.StdOut.WriteLine("    store");
            context.StdOut.WriteLine();
            context.StdOut.WriteLine("    --version, version");
            context.StdOut.WriteLine("    --help, -h, -?");
            context.StdOut.WriteLine();

            return Task.CompletedTask;
        }
    }
}
