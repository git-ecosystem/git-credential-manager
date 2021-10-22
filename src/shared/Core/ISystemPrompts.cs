
namespace GitCredentialManager
{
    /// <summary>
    /// Represents native system UI prompts.
    /// </summary>
    public interface ISystemPrompts
    {
        /// <summary>
        /// The parent window handle or ID. Used for correctly positioning and parenting system dialogs.
        /// </summary>
        /// <remarks>This value is platform specific.</remarks>
        object ParentWindowId { get; set; }

        /// <summary>
        /// Show a basic credential prompt using native system UI.
        /// </summary>
        /// <param name="resource">The name or URL of the resource to collect credentials for.</param>
        /// <param name="userName">Optional pre-filled username.</param>
        /// <param name="credential">The captured basic credential.</param>
        /// <returns>True if the user completes the dialog, false otherwise.</returns>
        bool ShowCredentialPrompt(string resource, string userName, out ICredential credential);
    }
}
