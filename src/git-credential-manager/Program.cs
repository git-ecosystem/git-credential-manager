using System;

namespace Microsoft.Git.CredentialManager
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            int exitCode = Application.RunAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
            Environment.Exit(exitCode);
        }
    }
}
