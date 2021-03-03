// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;

namespace Microsoft.Git.CredentialManager.Tests.Objects
{
    public class TestSettings : ISettings
    {
        public TestEnvironment Environment { get; set; }

        public TestGitConfiguration GitConfiguration { get; set; }

        public bool IsDebuggingEnabled { get; set; }

        public bool IsTerminalPromptsEnabled { get; set; } = true;

        public bool IsInteractionAllowed { get; set; } = true;

        public string Trace { get; set; }

        public bool IsSecretTracingEnabled { get; set; }

        public bool IsMsalTracingEnabled { get; set; }

        public string ProviderOverride { get; set; }

        public string LegacyAuthorityOverride { get; set; }

        public bool IsWindowsIntegratedAuthenticationEnabled { get; set; } = true;

        public bool IsCertificateVerificationEnabled { get; set; } = true;

        public ProxyConfiguration ProxyConfiguration { get; set; }

        public string ParentWindowId { get; set; }

        public string CredentialNamespace { get; set; } = "git-test";

        public string CredentialBackingStore { get; set; }

        #region ISettings

        public bool TryGetSetting(string envarName, string section, string property, out string value)
        {
            value = null;

            if (Environment?.Variables.TryGetValue(envarName, out value) ?? false)
            {
                return true;
            }

            if (GitConfiguration?.TryGet(section, property, out value) ?? false)
            {
                return true;
            }

            return false;
        }

        public IEnumerable<string> GetSettingValues(string envarName, string section, string property)
        {
            string envarValue = null;
            if (Environment?.Variables.TryGetValue(envarName, out envarValue) ?? false)
            {
                yield return envarValue;
            }

            foreach (string scope in RemoteUri.GetGitConfigurationScopes())
            {
                string key = $"{section}.{scope}.{property}";

                IList<string> configValues = null;
                if (GitConfiguration?.Dictionary.TryGetValue(key, out configValues) ?? false)
                {
                    if (configValues.Count > 0)
                    {
                        yield return configValues[0];
                    }
                }
            }
        }

        public string RepositoryPath { get; set; }

        public Uri RemoteUri { get; set; }

        bool ISettings.IsDebuggingEnabled => IsDebuggingEnabled;

        bool ISettings.IsTerminalPromptsEnabled => IsTerminalPromptsEnabled;

        bool ISettings.IsInteractionAllowed => IsInteractionAllowed;

        bool ISettings.GetTracingEnabled(out string value)
        {
            value = Trace;
            return Trace != null;
        }

        bool ISettings.IsSecretTracingEnabled => IsSecretTracingEnabled;

        bool ISettings.IsMsalTracingEnabled => IsMsalTracingEnabled;

        string ISettings.ProviderOverride => ProviderOverride;

        string ISettings.LegacyAuthorityOverride => LegacyAuthorityOverride;

        bool ISettings.IsWindowsIntegratedAuthenticationEnabled => IsWindowsIntegratedAuthenticationEnabled;

        bool ISettings.IsCertificateVerificationEnabled => IsCertificateVerificationEnabled;

        ProxyConfiguration ISettings.GetProxyConfiguration()
        {
            return ProxyConfiguration;
        }

        string ISettings.ParentWindowId => ParentWindowId;

        string ISettings.CredentialNamespace => CredentialNamespace;

        string ISettings.CredentialBackingStore => CredentialBackingStore;

        #endregion

        #region IDisposable

        void IDisposable.Dispose() { }

        #endregion
    }
}
