namespace Microsoft.Git.CredentialManager
{
    public interface ISessionManager
    {
        /// <summary>
        /// Determine if the current session has access to a desktop/can display UI.
        /// </summary>
        /// <returns>True if the session can display UI, false otherwise.</returns>
        bool IsDesktopSession { get; }
    }
}
