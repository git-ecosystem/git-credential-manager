using System;
using System.Collections.Generic;

namespace GitCredentialManager.Tests.Objects
{
    public class TestSettings : ISettings
    {
        public TestEnvironment Environment { get; set; }

        public TestGitConfiguration GitConfiguration { get; set; }

        public bool IsDebuggingEnabled { get; set; }

        public bool IsTerminalPromptsEnabled { get; set; } = true;

        public bool IsGuiPromptsEnabled { get; set; } = true;

        public bool IsInteractionAllowed { get; set; } = true;

        public string Trace { get; set; }

        public bool IsSecretTracingEnabled { get; set; }

        public bool IsMsalTracingEnabled { get; set; }

        public string ProviderOverride { get; set; }

        public string LegacyAuthorityOverride { get; set; }

        public bool IsWindowsIntegratedAuthenticationEnabled { get; set; } = true;

        public bool IsCertificateVerificationEnabled { get; set; } = true;

        public bool AutomaticallyUseClientCertificates { get; set; }

        public ProxyConfiguration ProxyConfiguration { get; set; }

        public string ParentWindowId { get; set; }

        public string CredentialNamespace { get; set; } = "git-test";

        public string CredentialBackingStore { get; set; }

        public string CustomCertificateBundlePath { get; set; }

        public string CustomCookieFilePath { get; set; }

        public TlsBackend TlsBackend { get; set; }

        public bool UseCustomCertificateBundleWithSchannel { get; set; }

        public int AutoDetectProviderTimeout { get; set; } = Constants.DefaultAutoDetectProviderTimeoutMs;

        public bool UseMsAuthDefaultAccount { get; set; }

        public Trace2Settings GetTrace2Settings()
        {
            return new Trace2Settings()
            {
                FormatTargetsAndValues = new Dictionary<Trace2FormatTarget, string>()
                {
                    { Trace2FormatTarget.Event, "foo" }
                }
            };
        }

        #region ISettings

        public bool TryGetSetting(string envarName, string section, string property, out string value)
        {
            value = null;

            if (Environment?.Variables.TryGetValue(envarName, out value) ?? false)
            {
                return true;
            }

            if (RemoteUri != null)
            {
                foreach (string scope in RemoteUri.GetGitConfigurationScopes())
                {
                    string key = $"{section}.{scope}.{property}";
                    if (GitConfiguration?.TryGet(key, false, out value) ?? false)
                    {
                        return true;
                    }
                }
            }

            if (GitConfiguration?.TryGet($"{section}.{property}", false, out value) ?? false)
            {
                return true;
            }

            return false;
        }

        public bool TryGetPathSetting(string envarName, string section, string property, out string value)
        {
            return TryGetSetting(envarName, section, property, out value);
        }

        public IEnumerable<string> GetSettingValues(string envarName, string section, string property, bool isPath)
        {
            string envarValue = null;
            if (Environment?.Variables.TryGetValue(envarName, out envarValue) ?? false)
            {
                yield return envarValue;
            }

            IEnumerable<string> configValues;
            if (RemoteUri != null)
            {
                foreach (string scope in RemoteUri.GetGitConfigurationScopes())
                {
                    string key = $"{section}.{scope}.{property}";

                    configValues = GitConfiguration.GetAll(key);
                    foreach (string value in configValues)
                    {
                        yield return value;
                    }
                }
            }

            configValues = GitConfiguration.GetAll($"{section}.{property}");
            foreach (string value in configValues)
            {
                yield return value;
            }
        }

        public string RepositoryPath { get; set; }

        public Uri RemoteUri { get; set; }

        bool ISettings.IsDebuggingEnabled => IsDebuggingEnabled;

        bool ISettings.IsTerminalPromptsEnabled => IsTerminalPromptsEnabled;

        bool ISettings.IsGuiPromptsEnabled
        {
            get => IsGuiPromptsEnabled;
            set => IsGuiPromptsEnabled = value;
        }

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

        string ISettings.CustomCertificateBundlePath => CustomCertificateBundlePath;

        string ISettings.CustomCookieFilePath => CustomCookieFilePath;

        TlsBackend ISettings.TlsBackend => TlsBackend;

        bool ISettings.UseCustomCertificateBundleWithSchannel => UseCustomCertificateBundleWithSchannel;

        int ISettings.AutoDetectProviderTimeout => AutoDetectProviderTimeout;

        bool ISettings.UseMsAuthDefaultAccount => UseMsAuthDefaultAccount;

        bool ISettings.UseSoftwareRendering => false;

        #endregion

        #region IDisposable

        void IDisposable.Dispose() { }

        #endregion
    }
}
