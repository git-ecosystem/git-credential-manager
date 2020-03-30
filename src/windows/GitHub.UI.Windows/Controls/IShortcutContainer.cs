namespace GitHub.UI.Controls
{
    /// <summary>
    /// Implemented by controls that need to control whether they support 
    /// keyboard shortcuts being raised from them or not.
    /// </summary>
    public interface IShortcutContainer
    {
        bool SupportsKeyboardShortcuts { get; set; }
    }
}
