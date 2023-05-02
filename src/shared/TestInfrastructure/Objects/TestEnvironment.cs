using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GitCredentialManager.Tests.Objects
{
    public class TestEnvironment : IEnvironment
    {
        private readonly IFileSystem _fileSystem;
        private readonly IEqualityComparer<string> _pathComparer;
        private readonly IEqualityComparer<string> _envarComparer;
        private readonly string _envPathSeparator;

        public TestEnvironment(IFileSystem fileSystem = null, string envPathSeparator = null, IEqualityComparer<string> pathComparer = null, IEqualityComparer<string> envarComparer = null)
        {
            _fileSystem = fileSystem ?? new TestFileSystem();

            // Use the current platform separators and comparison types by default
            _envPathSeparator = envPathSeparator ?? (PlatformUtils.IsWindows() ? ";" : ":");

            _envarComparer = envarComparer ??
                             (PlatformUtils.IsWindows()
                                 ? StringComparer.OrdinalIgnoreCase
                                 : StringComparer.Ordinal);

            _pathComparer = pathComparer ??
                            (PlatformUtils.IsLinux()
                                ? StringComparer.Ordinal
                                : StringComparer.OrdinalIgnoreCase);

            Variables = new Dictionary<string, string>(_envarComparer);
            Symlinks = new Dictionary<string, string>(_pathComparer);
        }

        public IDictionary<string, string> Variables { get; set; }

        public IDictionary<string, string> Symlinks { get; set; }

        public IList<string> Path
        {
            get
            {
                if (Variables.TryGetValue("PATH", out string value))
                {
                    return value.Split(new[] {_envPathSeparator}, StringSplitOptions.RemoveEmptyEntries);
                }

                return new string[0];
            }

            set => Variables["PATH"] = string.Join(_envPathSeparator, value);
        }

        #region IEnvironment

        IReadOnlyDictionary<string, string> IEnvironment.Variables => new ReadOnlyDictionary<string, string>(Variables);

        bool IEnvironment.IsDirectoryOnPath(string directoryPath)
        {
            return Path.Any(x => _pathComparer.Equals(x, directoryPath));
        }

        public void AddDirectoryToPath(string directoryPath, EnvironmentVariableTarget target)
        {
            Path.Add(directoryPath);

            // Update envar
            Variables["PATH"] = string.Join(_envPathSeparator, Path);
        }

        public void RemoveDirectoryFromPath(string directoryPath, EnvironmentVariableTarget target)
        {
            Path.Remove(directoryPath);

            // Update envar
            Variables["PATH"] = string.Join(_envPathSeparator, Path);
        }

        public bool TryLocateExecutable(string program, out string path)
        {
            if (Variables.TryGetValue("PATH", out string pathValue))
            {
                string[] paths = pathValue.Split(new[]{_envPathSeparator}, StringSplitOptions.None);
                foreach (var basePath in paths)
                {
                    string candidatePath = System.IO.Path.Combine(basePath, program);
                    if (_fileSystem.FileExists(candidatePath))
                    {
                        path = candidatePath;
                        return true;
                    }
                }
            }

            path = null;
            return false;
        }

        public void SetEnvironmentVariable(string variable, string value,
            EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
        {
            if (Variables.Keys.Contains(variable)) return;
            Environment.SetEnvironmentVariable(variable, value, target);
            Variables.Add(variable, value);
        }

        public void Refresh()
        {
            // Nothing to do!
        }

        #endregion
    }
}
