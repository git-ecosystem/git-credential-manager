// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager.Tests.Objects
{
    public class TestGit : IGit
    {
        public TestGitConfiguration SystemConfiguration { get; } = new TestGitConfiguration();
        public TestGitConfiguration GlobalConfiguration { get; } = new TestGitConfiguration();
        public TestGitConfiguration LocalConfiguration { get; } = new TestGitConfiguration();

        #region IGit

        IGitConfiguration IGit.GetConfiguration(GitConfigurationLevel level)
        {
            switch (level)
            {
                case GitConfigurationLevel.All:
                    IDictionary<string, IList<string>> mergedConfigDict =
                        MergeDictionaries(
                            SystemConfiguration.Dictionary,
                            GlobalConfiguration.Dictionary,
                            LocalConfiguration.Dictionary);
                    return new TestGitConfiguration(mergedConfigDict);
                case GitConfigurationLevel.ProgramData:
                case GitConfigurationLevel.Xdg:
                    return new TestGitConfiguration();
                case GitConfigurationLevel.System:
                    return SystemConfiguration;
                case GitConfigurationLevel.Global:
                    return GlobalConfiguration;
                case GitConfigurationLevel.Local:
                    return LocalConfiguration;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, $"Unknown {nameof(GitConfigurationLevel)}");
            }
        }

        Process IGit.CreateProcess(string args) => new Process();

        Task<IDictionary<string, string>> IGit.InvokeHelperAsync(string args, IDictionary<string, string> standardInput)
        {
            throw new NotImplementedException();
        }

        #endregion

        private static IDictionary<string, IList<string>> MergeDictionaries(params IDictionary<string, IList<string>>[] dictionaries)
        {
            var result = new Dictionary<string, IList<string>>(GitConfigurationKeyComparer.Instance);

            foreach (IDictionary<string, IList<string>> dict in dictionaries)
            {
                foreach (var kvp in dict)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }

            return result;
        }
    }
}
