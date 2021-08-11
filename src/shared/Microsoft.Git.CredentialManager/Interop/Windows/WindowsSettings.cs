// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Git.CredentialManager.Interop.Windows
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

        protected override bool TryGetExternalDefault(string section, string property, out string value)
        {
            value = null;

#if NETFRAMEWORK
            // Check for machine (HKLM) registry keys that match the Git configuration name.
            // These can be set by system administrators via Group Policy, so make useful defaults.
            using (Win32.RegistryKey configKey = Win32.Registry.LocalMachine.OpenSubKey(Constants.WindowsRegistry.HKConfigurationPath))
            {
                if (configKey is null)
                {
                    // No configuration key exists
                    return false;
                }

                string name = $"{section}.{property}";
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
            return base.TryGetExternalDefault(section, property, out value);
#endif
        }
    }
}
