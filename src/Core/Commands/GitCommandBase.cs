using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Threading.Tasks;

namespace GitCredentialManager.Commands
{
    /// <summary>
    /// Represents a command which selects a <see cref="IHostProvider"/> from a <see cref="IHostProviderRegistry"/>
    /// based on the <see cref="GitRequest"/> from standard input, and interacts with a <see cref="GitCredential"/>.
    /// </summary>
    public abstract class GitCommandBase : Command
    {
        private readonly IHostProviderRegistry _hostProviderRegistry;

        protected GitCommandBase(ICommandContext context, string name, string description, IHostProviderRegistry hostProviderRegistry)
            : base(name, description)
        {
            EnsureArgument.NotNull(hostProviderRegistry, nameof(hostProviderRegistry));
            EnsureArgument.NotNull(context, nameof(context));

            Context = context;
            _hostProviderRegistry = hostProviderRegistry;

            this.SetHandler(ExecuteAsync);
        }

        protected ICommandContext Context { get; }

        internal async Task ExecuteAsync()
        {
            using var _ = Trace2.StartRegion("git_cmd", "run");
            Trace2.WriteData("git_cmd", "name", Name);

            Context.Trace.WriteLine($"Start '{Name}' command...");

            // Parse standard input arguments
            IDictionary<string, IList<string>> inputDict;
            using (Trace2.StartRegion("git_cmd", "parse_input"))
            {
                // git-credential treats the keys as case-sensitive; so should we.
                inputDict = await Context.Streams.In.ReadMultiDictionaryAsync(StringComparer.Ordinal);
            }
            var request = new GitRequest(inputDict);

            // Validate minimum arguments are present
            EnsureMinimumRequest(request);

            // Set the remote URI to scope settings to throughout the process from now on
            Context.Settings.RemoteUri = request.GetRemoteUri();

            // Determine the host provider
            Context.Trace.WriteLine("Detecting host provider for request:");
            Context.Trace.WriteDictionarySecrets(inputDict, new []{ "password" }, StringComparer.OrdinalIgnoreCase);
            IHostProvider provider;
            using (Trace2.StartRegion("git_cmd", "resolve_provider"))
            {
                provider = await _hostProviderRegistry.GetProviderAsync(request);

                Trace2.WriteData("git_cmd", "provider/id", provider.Id);
                Trace2.WriteData("git_cmd", "provider/name", provider.Name);
            }
            Context.Trace.WriteLine($"Host provider '{provider.Name}' was selected.");

            await ExecuteInternalAsync(request, provider);

            Context.Trace.WriteLine($"End '{Name}' command...");
        }

        protected virtual void EnsureMinimumRequest(GitRequest request)
        {
            if (request.Protocol is null)
            {
                throw new InvalidOperationException("Missing 'protocol' request argument");
            }

            if (string.IsNullOrWhiteSpace(request.Protocol))
            {
                throw new InvalidOperationException(
                    "Invalid 'protocol' request argument (cannot be empty)");
            }

            if (request.Host is null)
            {
                throw new InvalidOperationException("Missing 'host' request argument");
            }

            if (string.IsNullOrWhiteSpace(request.Host))
            {
                throw new InvalidOperationException(
                    "Invalid 'host' request argument (cannot be empty)");
            }
        }

        /// <summary>
        /// Execute the command using the given <see cref="GitRequest"/> and <see cref="IHostProvider"/>.
        /// </summary>
        /// <param name="request">Input arguments of the current Git credential query.</param>
        /// <param name="provider">Host provider for the current <see cref="GitRequest"/>.</param>
        /// <returns>Awaitable task for the command execution.</returns>
        protected abstract Task ExecuteInternalAsync(GitRequest request, IHostProvider provider);
    }
}
