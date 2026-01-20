using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Avalonia.Markup.Xaml.MarkupExtensions;

namespace GitCredentialManager.Interop.Linux;

public class LinuxSettings : Settings
{
    private readonly ITrace _trace;
    private readonly IFileSystem _fs;

    private IDictionary<string, string> _extConfigCache;

    /// <summary>
    /// Reads settings from Git configuration, environment variables, and defaults from the
    /// /etc/git-credential-manager.d app configuration directory.
    /// </summary>
    public LinuxSettings(IEnvironment environment, IGit git, ITrace trace, IFileSystem fs)
        : base(environment, git)
    {
        EnsureArgument.NotNull(trace, nameof(trace));
        EnsureArgument.NotNull(fs, nameof(fs));

        _trace = trace;
        _fs = fs;

        PlatformUtils.EnsureLinux();
    }

    protected internal override bool TryGetExternalDefault(string section, string scope, string property, out string value)
    {
        value = null;

        _extConfigCache ??= ReadExternalConfiguration();

        string name = string.IsNullOrWhiteSpace(scope)
            ? $"{section}.{property}"
            : $"{section}.{scope}.{property}";

        // Check if the setting exists in the configuration
        if (!_extConfigCache?.TryGetValue(name, out value) ?? false)
        {
            // No property exists (or failed to read config)
            return false;
        }

        _trace.WriteLine($"Default setting found in app configuration directory: {name}={value}");
        return true;
    }

    private IDictionary<string, string> ReadExternalConfiguration()
    {
        try
        {
            // Check for system-wide config files in /etc/git-credential-manager/config.d and concatenate them together
            // in alphabetical order to form a single configuration.
            const string configDir = Constants.LinuxAppDefaultsDirectoryPath;
            if (!_fs.DirectoryExists(configDir))
            {
                // No configuration directory exists
                return null;
            }

            // Get all the files in the configuration directory
            IEnumerable<string> files = _fs.EnumerateFiles(configDir, "*");

            // Read the contents of each file and concatenate them together
            var combinedFile = new StringBuilder();
            foreach (string file in files)
            {
                using Stream stream = _fs.OpenFileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var reader = new StreamReader(stream);
                string contents = reader.ReadToEnd();
                combinedFile.Append(contents);
                combinedFile.Append('\n');
            }

            var parser = new LinuxConfigParser(_trace);

            return parser.Parse(combinedFile.ToString());
        }
        catch (Exception ex)
        {
            // Reading defaults is not critical to the operation of the application
            // so we can ignore any errors and just log the failure.
            _trace.WriteLine("Failed to read default setting from app configuration directory.");
            _trace.WriteException(ex);
            return null;
        }
    }
}