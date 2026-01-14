using System;
using System.Collections.Generic;

namespace GitCredentialManager.Interop.MacOS
{
    /// <summary>
    /// Reads settings from Git configuration, environment variables, and defaults from the system.
    /// </summary>
    public class MacOSSettings : Settings
    {
        private readonly ITrace _trace;

        public MacOSSettings(IEnvironment environment, IGit git, ITrace trace)
            : base(environment, git)
        {
            EnsureArgument.NotNull(trace, nameof(trace));
            _trace = trace;

            PlatformUtils.EnsureMacOS();
        }

        protected override bool TryGetExternalDefault(string section, string scope, string property, out string value)
        {
            value = null;

            try
            {
                // Check for app default preferences for our bundle ID.
                // Defaults can be deployed system administrators via device management profiles.
                var prefs = new MacOSPreferences(Constants.MacOSBundleId);
                IDictionary<string, string> dict = prefs.GetDictionary("configuration");

                if (dict is null)
                {
                    // No configuration key exists
                    return false;
                }

                // Wrap the raw dictionary in one configured with the Git configuration key comparer.
                // This means we can use the same key comparison rules as Git in our configuration plist dict,
                // That is, sections and names are insensitive to case, but the scope is case-sensitive.
                var config = new Dictionary<string, string>(dict, GitConfigurationKeyComparer.Instance);

                string name = string.IsNullOrWhiteSpace(scope)
                    ? $"{section}.{property}"
                    : $"{section}.{scope}.{property}";

                if (!config.TryGetValue(name, out value))
                {
                    // No property exists
                    return false;
                }

                _trace.WriteLine($"Default setting found in app preferences: {name}={value}");
                return true;
            }
            catch (Exception ex)
            {
                // Reading defaults is not critical to the operation of the application
                // so we can ignore any errors and just log the failure.
                _trace.WriteLine("Failed to read default setting from app preferences.");
                _trace.WriteException(ex);
                return false;
            }
        }
    }
}
