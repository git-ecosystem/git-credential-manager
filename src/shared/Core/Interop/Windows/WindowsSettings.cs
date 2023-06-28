
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

        protected override bool TryGetExternalDefault(string section, string scope, string property, out string value)
        {
            value = null;

#if NETFRAMEWORK
            // Check for machine (HKLM) registry keys that match the Git configuration name.
            // These can be set by system administrators via Group Policy, so make useful defaults.
            using (Microsoft.Win32.RegistryKey configKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(Constants.WindowsRegistry.HKConfigurationPath))
            {
                if (configKey is null)
                {
                    // No configuration key exists
                    return false;
                }

                string name = string.IsNullOrWhiteSpace(scope)
                    ? $"{section}.{property}"
                    : $"{section}.{scope}.{property}";

                object registryValue = configKey.GetValue(name);
                if (registryValue is null)
                {
                    // No property exists
                    return false;
                }

                value = registryValue.ToString();
                _trace.WriteLine($"Default setting found in registry: {name}={value}");

                return true;
            }
#else
            return base.TryGetExternalDefault(section, scope, property, out value);
#endif
        }
    }
}
