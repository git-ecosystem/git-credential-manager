using System.CommandLine;

namespace GitCredentialManager.Commands;

/// <summary>
/// Advertise the Git credential helper protocol capabilities that this
/// credential manager understands.
/// </summary>
/// <remarks>
/// <para>
/// Implements the <c>capability</c> action defined in the CAPABILITY INPUT/OUTPUT
/// FORMAT section of <see href="https://git-scm.com/docs/git-credential">git-credential(1)</see>.
/// </para>
/// <para>
/// Unlike <c>get</c> / <c>store</c> / <c>erase</c> this action does not read
/// the standard credential key/value protocol from standard input; it writes
/// a fixed-format response to standard output and exits:
/// </para>
/// <code>
/// version 0
/// capability &lt;name&gt;
/// ...
/// </code>
/// <para>
/// Git treats a non-zero exit, or a first line that does not begin with
/// <c>version </c>, as a signal that the helper supports no capabilities.
/// </para>
/// </remarks>
public class CapabilityCommand : Command
{
    /// <summary>
    /// The Git credential helper capability protocol version this helper speaks.
    /// </summary>
    private const int ProtocolVersion = 0;

    private readonly ICommandContext _context;

    public CapabilityCommand(ICommandContext context)
        : base("capability", "[Git] Advertise supported credential helper protocol capabilities")
    {
        EnsureArgument.NotNull(context, nameof(context));
        _context = context;

        IsHidden = true;

        this.SetHandler(Execute);
    }

    internal void Execute()
    {
        _context.Trace.WriteLine("Start 'capability' command...");

        _context.Streams.Out.WriteLine($"version {ProtocolVersion}");

        foreach (string name in GitCapabilitiesUtils.ToProtocolNames(Constants.SupportedCapabilities))
        {
            _context.Streams.Out.WriteLine($"capability {name}");
        }

        _context.Trace.WriteLine("End 'capability' command...");
    }
}
