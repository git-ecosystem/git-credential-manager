// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;
using KnownEnvars = Microsoft.Git.CredentialManager.Constants.EnvironmentVariables;
using KnownGitCfg = Microsoft.Git.CredentialManager.Constants.GitConfiguration;
using GitCredCfg  = Microsoft.Git.CredentialManager.Constants.GitConfiguration.Credential;
using GitHttpCfg  = Microsoft.Git.CredentialManager.Constants.GitConfiguration.Http;

namespace Microsoft.Git.CredentialManager
{
    /// <summary>
    /// Component that represents settings for Git Credential Manager as found from the environment and Git configuration.
    /// </summary>
    public interface ISettings : IDisposable
    {
        /// <summary>
        /// Try and get the value of a specified setting as specified in the environment and Git configuration,
        /// with the environment taking precedence over Git.
        /// </summary>
        /// <param name="envarName">Optional environment variable name.</param>
        /// <param name="section">Optional Git configuration section name.</param>
        /// <param name="property">Git configuration property name. Required if <paramref name="section"/> is set, optional otherwise.</param>
        /// <param name="value">Value of the requested setting.</param>
        /// <returns>True if a setting value was found, false otherwise.</returns>
        bool TryGetSetting(string envarName, string section, string property, out string value);

        /// <summary>
        /// Try and get the all values of a specified setting as specified in the environment and Git configuration,
        /// in the correct order or precedence.
        /// </summary>
        /// <param name="envarName">Optional environment variable name.</param>
        /// <param name="section">Optional Git configuration section name.</param>
        /// <param name="property">Git configuration property name. Required if <paramref name="section"/> is set, optional otherwise.</param>
        /// <returns>All values for the specified setting, in order of precedence, or an empty collection if no such values are set.</returns>
        IEnumerable<string> GetSettingValues(string envarName, string section, string property);

        /// <summary>
        /// Git remote address that setting lookup is scoped to, or null if no remote URL has been discovered.
        /// </summary>
        Uri RemoteUri { get; set; }

        /// <summary>
        /// True if debugging is enabled, false otherwise.
        /// </summary>
        bool IsDebuggingEnabled { get; }

        /// <summary>
        /// True if terminal prompting is enabled, false otherwise.
        /// </summary>
        bool IsTerminalPromptsEnabled { get; }

        /// <summary>
        /// True if it is permitted to interact with the user, false otherwise.
        /// </summary>
        /// <remarks>
        /// If this value is false but interactivity is required to continue execution, the caller should
        /// abort the operation and report failure.
        /// </remarks>
        bool IsInteractionAllowed { get; }

        /// <summary>
        /// Get if tracing has been enabled, returning trace setting value in the out parameter.
        /// </summary>
        /// <param name="value">Trace setting value.</param>
        /// <returns>True if tracing is enabled, false otherwise.</returns>
        bool GetTracingEnabled(out string value);

        /// <summary>
        /// True if tracing of secrets and sensitive information is enabled, false otherwise.
        /// </summary>
        bool IsSecretTracingEnabled { get; }

        /// <summary>
        /// True if MSAL tracing is enabled, false otherwise.
        /// </summary>
        bool IsMsalTracingEnabled { get; }

        /// <summary>
        /// Get the host provider configured to override auto-detection if set, null otherwise.
        /// </summary>
        string ProviderOverride { get; }

        /// <summary>
        /// Get the authority name configured to override host provider auto-detection if set, null otherwise.
        /// </summary>
        string LegacyAuthorityOverride { get; }

        /// <summary>
        /// True if Windows Integrated Authentication (NTLM, Kerberos) should be detected and used if available, false otherwise.
        /// </summary>
        bool IsWindowsIntegratedAuthenticationEnabled { get; }

        /// <summary>
        /// True if certificate verification should occur, false otherwise.
        /// </summary>
        bool IsCertificateVerificationEnabled { get; }

        /// <summary>
        /// Get the proxy setting if configured, or null otherwise.
        /// </summary>
        /// <param name="isDeprecatedConfiguration">True if the proxy configuration method is deprecated, false otherwise.</param>
        /// <returns>Proxy setting, or null if not configured.</returns>
        Uri GetProxyConfiguration(out bool isDeprecatedConfiguration);

        /// <summary>
        /// The parent window handle/ID. Used to correctly position and parent dialogs generated by GCM.
        /// </summary>
        /// <remarks>This value is platform specific.</remarks>
        string ParentWindowId { get; }

        /// <summary>
        /// Credential storage namespace prefix.
        /// </summary>
        /// <remarks>The default value is "git" if unset.</remarks>
        string CredentialNamespace { get; }

        /// <summary>
        /// Credential backing store override.
        /// </summary>
        string CredentialBackingStore { get; }
    }

    public class Settings : ISettings
    {
        private readonly IEnvironment _environment;
        private readonly IGit _git;

        public Settings(IEnvironment environment, IGit git)
        {
            EnsureArgument.NotNull(environment, nameof(environment));
            EnsureArgument.NotNull(git, nameof(git));

            _environment = environment;
            _git = git;
        }

        public bool TryGetSetting(string envarName, string section, string property, out string value)
        {
            IEnumerable<string> allValues = GetSettingValues(envarName, section, property);

            value = allValues.FirstOrDefault();

            return value != null;
        }

        public IEnumerable<string> GetSettingValues(string envarName, string section, string property)
        {
            string value;

            if (envarName != null)
            {
                if (_environment.Variables.TryGetValue(envarName, out value))
                {
                    yield return value;
                }
            }

            if (section != null && property != null)
            {
                IGitConfiguration config = _git.GetConfiguration();

                if (RemoteUri != null)
                {
                    /*
                     * Look for URL scoped "section" configuration entries, starting from the most specific
                     * down to the least specific (stopping before the TLD).
                     *
                     * In a divergence from standard Git configuration rules, we also consider matching URL scopes
                     * without a scheme ("protocol://").
                     *
                     * For each level of scope, we look for an entry with the scheme included (the default), and then
                     * also one without it specified. This allows you to have one configuration scope for both "http" and
                     * "https" without needing to repeat yourself, for example.
                     *
                     * For example, starting with "https://foo.example.com/bar/buzz" we have:
                     *
                     *   1a. [section "https://foo.example.com/bar/buzz"]
                     *          property = value
                     *
                     *   1b. [section "foo.example.com/bar/buzz"]
                     *          property = value
                     *
                     *   2a. [section "https://foo.example.com/bar"]
                     *          property = value
                     *
                     *   2b. [section "foo.example.com/bar"]
                     *          property = value
                     *
                     *   3a. [section "https://foo.example.com"]
                     *          property = value
                     *
                     *   3b. [section "foo.example.com"]
                     *          property = value
                     *
                     *   4a. [section "https://example.com"]
                     *          property = value
                     *
                     *   4b. [section "example.com"]
                     *          property = value
                     *
                     */

                    // Enumerate all configuration entries with the correct section and property name
                    // and make a local copy of them here to avoid needing to call `TryGetValue` on the
                    // IGitConfiguration object multiple times in a loop below.
                    var configEntries = new Dictionary<string, string>();
                    config.Enumerate((entryName, entryValue) =>
                    {
                        string entrySection = entryName.TruncateFromIndexOf('.');
                        string entryProperty = entryName.TrimUntilLastIndexOf('.');

                        if (StringComparer.OrdinalIgnoreCase.Equals(entrySection, section) &&
                            StringComparer.OrdinalIgnoreCase.Equals(entryProperty, property))
                        {
                            configEntries[entryName] = entryValue;
                        }

                        // Continue the enumeration
                        return true;
                    });

                    foreach (string scope in RemoteUri.GetGitConfigurationScopes())
                    {
                        string queryName = $"{section}.{scope}.{property}";
                        // Look for a scoped entry that includes the scheme "protocol://example.com" first as this is more specific
                        if (configEntries.TryGetValue(queryName, out value))
                        {
                            yield return value;
                        }

                        // Now look for a scoped entry that omits the scheme "example.com" second as this is less specific
                        string scopeWithoutScheme = scope.TrimUntilIndexOf(Uri.SchemeDelimiter);
                        string queryWithSchemeName = $"{section}.{scopeWithoutScheme}.{property}";
                        if (configEntries.TryGetValue(queryWithSchemeName, out value))
                        {
                            yield return value;
                        }
                    }
                }

                /*
                 * Try to look for an un-scoped "section" property setting:
                 *
                 *    [section]
                 *        property = value
                 *
                 */
                if (config.TryGetValue($"{section}.{property}", out value))
                {
                    yield return value;
                }
            }
        }

        public Uri RemoteUri { get; set; }

        public bool IsDebuggingEnabled => _environment.Variables.GetBooleanyOrDefault(KnownEnvars.GcmDebug, false);

        public bool IsTerminalPromptsEnabled => _environment.Variables.GetBooleanyOrDefault(KnownEnvars.GitTerminalPrompts, true);

        public bool IsInteractionAllowed
        {
            get
            {
                const bool defaultValue = true;
                if (TryGetSetting(KnownEnvars.GcmInteractive, GitCredCfg.SectionName, GitCredCfg.Interactive, out string value))
                {
                    /*
                     * COMPAT: In the previous GCM we accepted the values 'auto', 'never', and 'always'.
                     *
                     * We've slightly changed the behaviour of this setting in GCM Core to essentially
                     * remove the 'always' option. The table below outlines the changes:
                     *
                     * ┌──────────┬───────────────────────────┬────────────────────┐
                     * │ Value(s) │ Old meaning               │ New meaning        │
                     * ┝━━━━━━━━━━┿━━━━━━━━━━━━━━━━━━━━━━━━━━━┿━━━━━━━━━━━━━━━━━━━━┥
                     * │ auto     │ Prompt if required        │ [unchanged]        │
                     * ├──────────┼───────────────────────────┼────────────────────┤
                     * │ never    │ Never prompt ─ fail if    │ [unchanged]        │
                     * │ false    │ interaction is required   │                    │
                     * ├──────────┼───────────────────────────┼────────────────────┤
                     * │ always   │ Always prompt ─ don't use │ Prompt if required │
                     * │ force    │ cached credentials        │                    │
                     * │ true     │                           │                    │
                     * └──────────┴───────────────────────────┴────────────────────┘
                     */
                    if (StringComparer.OrdinalIgnoreCase.Equals("never", value))
                    {
                        return false;
                    }

                    return value.ToBooleanyOrDefault(defaultValue);
                }

                return defaultValue;
            }
        }

        public bool GetTracingEnabled(out string value) => _environment.Variables.TryGetValue(KnownEnvars.GcmTrace, out value) && !value.IsFalsey();

        public bool IsSecretTracingEnabled => _environment.Variables.GetBooleanyOrDefault(KnownEnvars.GcmTraceSecrets, false);

        public bool IsMsalTracingEnabled => _environment.Variables.GetBooleanyOrDefault(Constants.EnvironmentVariables.GcmTraceMsAuth, false);

        public string ProviderOverride =>
            TryGetSetting(KnownEnvars.GcmProvider, GitCredCfg.SectionName, GitCredCfg.Provider, out string providerId) ? providerId : null;

        public string LegacyAuthorityOverride =>
            TryGetSetting(KnownEnvars.GcmAuthority, GitCredCfg.SectionName, GitCredCfg.Authority, out string authority) ? authority : null;

        public bool IsWindowsIntegratedAuthenticationEnabled =>
            !TryGetSetting(KnownEnvars.GcmAllowWia, GitCredCfg.SectionName, GitCredCfg.AllowWia, out string value) || value.ToBooleanyOrDefault(true);

        public bool IsCertificateVerificationEnabled
        {
            get
            {
                // Prefer environment variable
                if (_environment.Variables.TryGetValue(KnownEnvars.GitSslNoVerify, out string envarValue))
                {
                    return !envarValue.ToBooleanyOrDefault(false);
                }

                // Next try the equivalent Git configuration option
                if (TryGetSetting(null, KnownGitCfg.Http.SectionName, KnownGitCfg.Http.SslVerify, out string cfgValue))
                {
                    return cfgValue.ToBooleanyOrDefault(true);
                }

                // Safe default
                return true;
            }
        }

        public Uri GetProxyConfiguration(out bool isDeprecatedConfiguration)
        {
            isDeprecatedConfiguration = false;

            bool TryGetUriSetting(string envarName, string section, string property, out Uri uri)
            {
                IEnumerable<string> allValues = GetSettingValues(envarName, section, property);

                foreach (var value in allValues)
                {
                    if (Uri.TryCreate(value, UriKind.Absolute, out uri))
                    {
                        return true;
                    }
                }

                uri = null;
                return false;
            }

            /*
             * There are several different ways we support the configuration of a proxy.
             *
             * In order of preference:
             *
             *   1. GCM proxy Git configuration (deprecated)
             *        credential.httpsProxy
             *        credential.httpProxy
             *
             *   2. Standard Git configuration
             *        http.proxy
             *
             *   3. cURL environment variables
             *        HTTPS_PROXY
             *        HTTP_PROXY
             *        ALL_PROXY
             *
             *   4. GCM proxy environment variable (deprecated)
             *        GCM_HTTP_PROXY
             *
             * If the remote URI is HTTPS we check the HTTPS variants first, and fallback to the
             * non-secure HTTP options if not found.
             *
             * For HTTP URIs we only check the HTTP variants.
             *
             */

            bool isHttps = StringComparer.OrdinalIgnoreCase.Equals(Uri.UriSchemeHttps, RemoteUri?.Scheme);

            Uri proxyConfig;

            // 1. GCM proxy Git configuration (deprecated)
            if (isHttps && TryGetUriSetting(null, GitCredCfg.SectionName, GitCredCfg.HttpsProxy, out proxyConfig) ||
                TryGetUriSetting(null, GitCredCfg.SectionName, GitCredCfg.HttpProxy, out proxyConfig))
            {
                isDeprecatedConfiguration = true;
                return proxyConfig;
            }

            // 2. Standard Git configuration
            if (TryGetUriSetting(null, GitHttpCfg.SectionName, GitHttpCfg.Proxy, out proxyConfig))
            {
                return proxyConfig;
            }

            // 3. cURL environment variables
            if (isHttps && TryGetUriSetting(KnownEnvars.CurlHttpsProxy, null, null, out proxyConfig) ||
                TryGetUriSetting(KnownEnvars.CurlHttpProxy, null, null, out proxyConfig) ||
                TryGetUriSetting(KnownEnvars.CurlAllProxy, null, null, out proxyConfig))
            {
                return proxyConfig;
            }

            // 4. GCM proxy environment variable (deprecated)
            if (TryGetUriSetting(KnownEnvars.GcmHttpProxy, null, null, out proxyConfig))
            {
                isDeprecatedConfiguration = true;
                return proxyConfig;
            }

            return null;
        }

        public string ParentWindowId => _environment.Variables.TryGetValue(KnownEnvars.GcmParentWindow, out string parentWindowId) ? parentWindowId : null;

        public string CredentialNamespace =>
            TryGetSetting(KnownEnvars.GcmCredNamespace,
                KnownGitCfg.Credential.SectionName, KnownGitCfg.Credential.CredNamespace,
                out string @namespace)
                ? @namespace
                : Constants.DefaultCredentialNamespace;

        public string CredentialBackingStore =>
            TryGetSetting(
                KnownEnvars.GcmCredentialStore,
                KnownGitCfg.Credential.SectionName,
                KnownGitCfg.Credential.CredentialStore,
                out string credStore)
                ? credStore
                : null;

        #region IDisposable

        public void Dispose()
        {
            // Do nothing
        }

        #endregion
    }
}
