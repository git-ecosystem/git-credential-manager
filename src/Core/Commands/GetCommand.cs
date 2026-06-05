using System;
using System.Collections.Generic;
using System.Linq;
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

            if (response.IsCancelled)
            {
                // Provider declined to produce a credential. Tell Git to stop the
                // credential acquisition pipeline (no fallback interactive prompt)
                // via the `quit` protocol attribute. This avoids re-prompting a
                // user who has already explicitly cancelled in a GUI dialog.
                Context.Trace.WriteLine("Provider cancelled the credential request; emitting quit=1.");
                Context.Streams.Error.WriteLine("info: user cancelled the credential request.");
                Context.Streams.Out.WriteLine("quit=1");
                Context.Streams.Out.WriteLine();
                return;
            }

            if (response.IsYielded)
            {
                // Provider has nothing to contribute but does not want to stop the
                // pipeline. Emit an empty response (just the terminating blank line)
                // so Git proceeds to the next helper or its interactive prompt.
                Context.Trace.WriteLine("Provider yielded; emitting empty response.");
                Context.Streams.Out.WriteLine();
                return;
            }

            ICredential credential = response.Credential;

            // Negotiate capabilities by intersecting what Git advertised with what GCM supports.
            // Capability-gated output fields may only be emitted for capabilities in this set.
            GitCapabilities negotiated = request.Capabilities & Constants.SupportedCapabilities;
            IList<string> negotiatedNames = GitCapabilitiesUtils.ToProtocolNames(negotiated).ToList();

            // We use a scalar dictionary so that empty string values (notably the
            // empty username/password pair that signals Windows Integrated Authentication
            // to Git) round-trip correctly. Multi-value protocol fields such as
            // capability[] are written directly below.
            var output = new Dictionary<string, string>();

            // Echo protocol, host, and path back at Git
            if (request.Protocol != null)
            {
                output["protocol"] = request.Protocol;
            }
            if (request.Host != null)
            {
                output["host"] = request.Host;
            }
            if (request.Path != null)
            {
                output["path"] = request.Path;
            }

            // Return the credential to Git (may be empty/empty for WIA)
            output["username"] = credential.Account;
            output["password"] = credential.Password;

            // Write any additional output from the provider
            foreach (var kvp in response.AdditionalProperties)
            {
                output[kvp.Key] = kvp.Value;
            }

            Context.Trace.WriteLine("Writing credentials to output:");
            Context.Trace.WriteDictionarySecrets(output, new []{ "password" }, StringComparer.OrdinalIgnoreCase);
            if (negotiatedNames.Count > 0)
            {
                Context.Trace.WriteLine($"\tcapability[]={string.Join(",", negotiatedNames)}");
            }

            // Emit negotiated capabilities first, then the scalar fields.
            // Always use the multi-value capability[]= form per the protocol.
            foreach (string name in negotiatedNames)
            {
                Context.Streams.Out.WriteLine($"capability[]={name}");
            }

            // Write the scalar values (and the terminating blank line) to standard out.
            Context.Streams.Out.WriteDictionary(output);
        }
    }
}
