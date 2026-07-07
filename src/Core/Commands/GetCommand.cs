using System.IO;
using System.Threading.Tasks;

namespace GitCredentialManager.Commands
{
    /// <summary>
    /// Acquire a new <see cref="GitCredential"/> from a <see cref="IHostProvider"/>.
    /// </summary>
    public class GetCommand : GitCommandBase
    {
        public GetCommand(ICommandContext context, IHostProviderRegistry hostProviderRegistry)
            : base(context, "get", "[Git] Return a stored credential", hostProviderRegistry)
        {
            IsHidden = true;
        }

        protected override async Task ExecuteInternalAsync(GitRequest request, IHostProvider provider)
        {
            GitResponse response = await provider.GetCredentialAsync(request);
            TextWriter stdout = Context.Streams.Out;

            if (response.IsCancelled)
            {
                // Tell Git to stop the credential acquisition pipeline (no
                // fallback interactive prompt) via the `quit` protocol
                // attribute. This avoids re-prompting a user who has already
                // explicitly cancelled in a GUI dialog.
                Context.Trace.WriteLine("Provider cancelled the credential request; emitting quit=1.");
                Context.Streams.Error.WriteLine("info: user cancelled the credential request.");
                stdout.WriteLine("quit=1");
                stdout.WriteLine();
                return;
            }

            if (response.IsYielded)
            {
                // Empty response (just the terminating blank line) lets Git
                // proceed to the next helper or its interactive prompt.
                Context.Trace.WriteLine("Provider yielded; emitting empty response.");
                stdout.WriteLine();
                return;
            }

            ICredential credential = response.Credential;

            // Negotiate capabilities by intersecting what Git advertised with what GCM supports.
            // Capability-gated output fields may only be emitted for capabilities in this set.
            GitCapabilities negotiated = request.Capabilities & Constants.SupportedCapabilities;
            bool stateCapNegotiated = (negotiated & GitCapabilities.State) != 0;

            Context.Trace.WriteLine($"Git capability: {request.Capabilities}");
            Context.Trace.WriteLine($"GCM capabilities: {Constants.SupportedCapabilities}");
            Context.Trace.WriteLine($"Negotiated capabilities: {negotiated}");

            Context.Trace.WriteLine("Writing credentials to output:");

            //
            // Capabilities
            //
            foreach (string name in GitCapabilitiesUtils.ToProtocolNames(negotiated))
            {
                stdout.WriteLine($"capability[]={name}");
                Context.Trace.WriteLine($"\tcapability[]={name}");
            }

            //
            // Common arguments
            //
            if (request.Protocol != null)
            {
                stdout.WriteLine($"protocol={request.Protocol}");
                Context.Trace.WriteLine($"\tprotocol={request.Protocol}");
            }
            if (request.Host != null)
            {
                stdout.WriteLine($"host={request.Host}");
                Context.Trace.WriteLine($"\thost={request.Host}");
            }
            if (request.Path != null)
            {
                stdout.WriteLine($"path={request.Path}");
                Context.Trace.WriteLine($"\tpath={request.Path}");
            }

            //
            // Credential
            //
            stdout.WriteLine($"username={credential.Account}");
            Context.Trace.WriteLine($"\tusername={credential.Account}");
            stdout.WriteLine($"password={credential.Password}");
            Context.Trace.WriteLineSecrets("\tpassword={0}", new object[] { credential.Password });

            //
            // Custom additional properties
            //
            foreach (var kvp in response.AdditionalProperties)
            {
                stdout.WriteLine($"{kvp.Key}={kvp.Value}");
                Context.Trace.WriteLine($"\t{kvp.Key}={kvp.Value}");
            }

            if (response.IsContinue)
            {
                if (stateCapNegotiated)
                {
                    stdout.WriteLine($"{Constants.CredentialProtocol.ContinueKey}=1");
                    Context.Trace.WriteLine($"\t{Constants.CredentialProtocol.ContinueKey}=1");
                }
                else
                {
                    // Dropping continue=1 changes the auth semantics: Git will treat this
                    // credential as final and likely fail on the next 401!
                    Context.Trace.WriteLine(
                        "WARNING: Provider set continue=1 but the 'state' capability was not " +
                        "negotiated with Git. Dropping continue=1; multistage authentication " +
                        "will not work and the credential will likely fail on the next 401.");
                }
            }

            if (response.State.Count > 0)
            {
                if (stateCapNegotiated)
                {
                    foreach (var kvp in response.State)
                    {
                        string line =
                            $"{Constants.CredentialProtocol.StateKey}[]=" +
                            $"{Constants.CredentialProtocol.GcmStatePrefix}{kvp.Key}={kvp.Value}";
                        stdout.WriteLine(line);
                        Context.Trace.WriteLine($"\t{line}");
                    }
                }
                else
                {
                    Context.Trace.WriteLine(
                        $"Provider set {response.State.Count} state entries but the 'state' " +
                        "capability was not negotiated with Git; dropping.");
                }
            }

            // Terminating blank line per the credential protocol.
            stdout.WriteLine();
        }
    }
}
