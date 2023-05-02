using System;
using System.Collections.Generic;
using System.Linq;

namespace GitCredentialManager.Interop.Posix
{
    public class PosixEnvironment : EnvironmentBase
    {
        public PosixEnvironment(IFileSystem fileSystem)
            : base(fileSystem) { }

        internal PosixEnvironment(IFileSystem fileSystem, IReadOnlyDictionary<string, string> variables)
            : base(fileSystem, variables) { }

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

        #endregion

        protected override IReadOnlyDictionary<string, string> GetCurrentVariables()
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
