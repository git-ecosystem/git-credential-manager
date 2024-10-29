using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitCredentialManager;

/// <summary>
/// StreamReader that does NOT consider a lone carriage-return as a new-line character,
/// only a line-feed or carriage-return immediately followed by a line-feed.
/// <para/>
/// The only major operating system that uses a lone carriage-return as a new-line character
/// is the classic Macintosh OS (before OS X), which is not supported by Git.
/// </summary>
public class GitStreamReader : StreamReader
{
    public GitStreamReader(Stream stream, Encoding encoding) : base(stream, encoding) { }

    public override string ReadLine()
    {
#if NETFRAMEWORK
        return ReadLineAsync().ConfigureAwait(false).GetAwaiter().GetResult();
#else
        return ReadLineAsync(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
#endif
    }

#if NETFRAMEWORK
    public override async Task<string> ReadLineAsync()
#else
    public override async ValueTask<string> ReadLineAsync(CancellationToken cancellationToken)
#endif
    {
        int nr;
        var sb = new StringBuilder();
        var buffer = new char[1];
        bool lastWasCR = false;

        while ((nr = await base.ReadAsync(buffer, 0, 1).ConfigureAwait(false)) > 0)
        {
            char c = buffer[0];

            // Only treat a line-feed as a new-line character.
            // Carriage-returns alone are NOT considered new-line characters.
            if (c == '\n')
            {
                if (lastWasCR)
                {
                    // If the last character was a carriage-return we should remove it from the string builder
                    // since together with this line-feed it is considered a new-line character.
                    sb.Length--;
                }

                // We have a new-line character, so we should stop reading.
                break;
            }

            lastWasCR = c == '\r';

            sb.Append(c);
        }

        if (sb.Length == 0 && nr == 0)
        {
            return null;
        }

        return sb.ToString();
    }
}
