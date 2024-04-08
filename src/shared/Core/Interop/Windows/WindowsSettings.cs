
namespace GitCredentialManager.Interop.Windows
{
    /// <summary>
    /// Reads settings from Git configuration, environment variables, and defaults from the Windows Registry.
    /// </summary>
    public class WindowsSettings : Settings
    {
        private readonly ITrace _trace;

        public WindowsSettings(IEnvironment environment, IGit git, ITrace trace)
            : base(environment, git)
        {
            EnsureArgument.NotNull(trace, nameof(trace));
            _trace = trace;

            PlatformUtils.EnsureWindows();
        }
    }
}
