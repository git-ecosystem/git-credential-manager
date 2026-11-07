using System.Threading;

namespace GitCredentialManager.Authentication.Entra;

/// <summary>
/// Ambient description of what a credential request is for (for example the remote URL),
/// shown in interactive prompts so the user can tell which repository is asking.
/// </summary>
public static class AuthPromptContext
{
    private static readonly AsyncLocal<string> Current = new();

    public static string Description
    {
        get => Current.Value;
        set => Current.Value = value;
    }
}
