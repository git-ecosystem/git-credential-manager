using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GitCredentialManager.Interop.Posix
{
    public class PosixEnvironment : EnvironmentBase
    {
        public PosixEnvironment(IFileSystem fileSystem) : base(fileSystem)
        {
            Variables = GetCurrentVariables();
        }

        #region EnvironmentBase

        public override void AddDirectoryToPath(string directoryPath, EnvironmentVariableTarget target)
        {
            throw new NotImplementedException();
        }

        public override void RemoveDirectoryFromPath(string directoryPath, EnvironmentVariableTarget target)
        {
            throw new NotImplementedException();
        }

        protected override string[] SplitPathVariable(string value)
        {
            return value.Split(':');
        }

        public override bool TryLocateExecutable(string program, out string path)
        {
            // The "which" utility scans over the PATH and does not include the current working directory
            // (unlike the equivalent "where.exe" on Windows), which is exactly what we want. Let's use it.
            const string whichPath = "/usr/bin/which";
            var psi = new ProcessStartInfo(whichPath, program)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            using (var where = new Process {StartInfo = psi})
            {
                where.Start();
                where.WaitForExit();

                switch (where.ExitCode)
                {
                    case 0: // found
                        string stdout = where.StandardOutput.ReadToEnd();
                        string[] results = stdout.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
                        path = results.First();
                        return true;

                    case 1: // not found
                        path = null;
                        return false;

                    default:
                        throw new Exception($"Unknown error locating '{program}' using {whichPath}. Exit code: {where.ExitCode}.");
                }
            }
        }

        #endregion

        private static IReadOnlyDictionary<string, string> GetCurrentVariables()
        {
            var dict = new Dictionary<string, string>();
            var variables = Environment.GetEnvironmentVariables();

            foreach (var key in variables.Keys)
            {
                if (key is string name && variables[key] is string value)
                {
                    dict[name] = value;
                }
            }

            return dict;
        }
    }
}
