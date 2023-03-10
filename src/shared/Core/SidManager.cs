using System;

namespace GitCredentialManager;

public class SidManager
{
    private const string SidEnvar = "GIT_TRACE2_PARENT_SID";

    public static string Sid { get; private set; }

    public static void CreateSid()
    {
        Sid = Environment.GetEnvironmentVariable(SidEnvar);

        if (!string.IsNullOrEmpty(Sid))
        {
            Sid = $"{Sid}/{Guid.NewGuid():D}";
        }
        else
        {
            // We are the root process; create our own 'root' SID
            Sid = Guid.NewGuid().ToString("D");
        }

        Environment.SetEnvironmentVariable(SidEnvar, Sid);
    }
}
