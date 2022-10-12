namespace GitCredentialManager
{
    public interface ISessionManager
    {
        /// <summary>
        /// Determine if the current session has access to a desktop/can display UI.
        /// </summary>
        /// <returns>True if the session can display UI, false otherwise.</returns>
        bool IsDesktopSession { get; }

        /// <summary>
        /// Determine if the current session has access to a web browser.
        /// </summary>
        /// <returns>True if the session can display a web browser, false otherwise.</returns>
        bool IsWebBrowserAvailable { get; }
    }
    
    public abstract class SessionManager : ISessionManager
    {
        public abstract bool IsDesktopSession { get; }

        public virtual bool IsWebBrowserAvailable => IsDesktopSession;
    }
}
