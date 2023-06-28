using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using GitCredentialManager.Interop.Posix;

namespace GitCredentialManager.Interop.MacOS
{
    public class MacOSEnvironment : PosixEnvironment
    {
        private ICollection<string> _pathsToIgnore;

        public MacOSEnvironment(IFileSystem fileSystem)
            : base(fileSystem) { }

        internal MacOSEnvironment(IFileSystem fileSystem, IReadOnlyDictionary<string, string> variables)
            : base(fileSystem, variables) { }

        public override bool TryLocateExecutable(string program, out string path)
        {
            if (_pathsToIgnore is null)
            {
                _pathsToIgnore = new List<string>();
                if (Variables.TryGetValue("HOMEBREW_PREFIX", out string homebrewPrefix))
                {
                    string homebrewGit = Path.Combine(homebrewPrefix, "Homebrew/Library/Homebrew/shims/shared/git");
                    _pathsToIgnore.Add(homebrewGit);
                }
            }
            return TryLocateExecutable(program, _pathsToIgnore, out path);
        }
    }
}
