using System.Text;

namespace GitCredentialManager;

public static class EncodingEx
{
    public static readonly Encoding UTF8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
}
