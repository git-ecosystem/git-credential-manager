// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Git.CredentialManager.Interop.Posix
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

        public override string LocateExecutable(string program)
        {
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

                if (where.ExitCode != 0)
                {
                    throw new Exception($"Failed to locate '{program}' using {whichPath}. Exit code: {where.ExitCode}.");
                }

                string stdout = where.StandardOutput.ReadToEnd();
                if (string.IsNullOrWhiteSpace(stdout))
                {
                    return null;
                }

                string[] results = stdout.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
                return results.FirstOrDefault();
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
