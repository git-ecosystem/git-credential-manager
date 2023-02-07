namespace GitCredentialManager;

public static class TraceUtils
{
    public static string FormatSource(string source, int sourceColumnMaxWidth)
    {
        int idx = 0;
        int maxlen = sourceColumnMaxWidth - 3;
        int srclen = source.Length;

        while (idx >= 0 && (srclen - idx) > maxlen)
        {
            idx = source.IndexOf('\\', idx + 1);
        }

        // If we cannot find a path separator which allows the path to be long enough, just truncate the file name
        if (idx < 0)
        {
            idx = srclen - maxlen;
        }

        return "..." + source.Substring(idx);
    }
}
