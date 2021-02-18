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
        public readonly TestGitConfiguration Configuration = new TestGitConfiguration();

        #region IGit

        IGitConfiguration IGit.GetConfiguration() => Configuration;

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
