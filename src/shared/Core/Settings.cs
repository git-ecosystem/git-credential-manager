using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using KnownEnvars = GitCredentialManager.Constants.EnvironmentVariables;
using KnownGitCfg = GitCredentialManager.Constants.GitConfiguration;
using GitCredCfg  = GitCredentialManager.Constants.GitConfiguration.Credential;
using GitHttpCfg  = GitCredentialManager.Constants.GitConfiguration.Http;

namespace GitCredentialManager
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
        /// Try and get the value of a specified setting as specified in the environment and Git configuration,
        /// with the environment taking precedence over Git. If the value is pulled from the Git configuration,
        /// it is returned as a canonical path.
        /// </summary>
        /// <param name="envarName">Optional environment variable name.</param>
        /// <param name="section">Optional Git configuration section name.</param>
        /// <param name="property">Git configuration property name. Required if <paramref name="section"/> is set, optional otherwise.</param>
        /// <param name="value">Value of the requested setting as a canonical path.</param>
        /// <returns>True if a setting value was found, false otherwise.</returns>
        bool TryGetPathSetting(string envarName, string section, string property, out string value);

        /// <summary>
        /// Try and get the all values of a specified setting as specified in the environment and Git configuration,
        /// in the correct order or precedence.
        /// </summary>
        /// <param name="envarName">Optional environment variable name.</param>
        /// <param name="section">Optional Git configuration section name.</param>
        /// <param name="property">Git configuration property name. Required if <paramref name="section"/> is set, optional otherwise.</param>
        /// <param name="isPath">Whether the returned values should be transformed into canonical paths.</param>
        /// <returns>All values for the specified setting, in order of precedence, or an empty collection if no such values are set.</returns>
        IEnumerable<string> GetSettingValues(string envarName, string section, string property, bool isPath);

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
        /// <returns>Proxy setting, or null if not configured.</returns>
        ProxyConfiguration GetProxyConfiguration();

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

        /// <summary>
        /// Optional path to a file containing one or more certificates that should
        /// be used *exclusively* when verifying server certificate chains.
        /// </summary>
        /// <remarks>The default value is null if unset.</remarks>
        string CustomCertificateBundlePath { get; }

        /// <summary>
        /// The SSL/TLS backend.
        /// </summary>
        TlsBackend TlsBackend { get; }

        /// <summary>
        /// True if, when using an schannel backend, using certificates from the
        /// CustomCertificateBundlePath is allowed.
        /// </summary>
        /// <remarks>The default value is false if unset.</remarks>
        bool UseCustomCertificateBundleWithSchannel { get; }

        /// <summary>
        /// Maximum number of milliseconds to wait for a network response when probing a remote URL for the purpose
        /// of host provider auto-detection. Use a zero or negative value to disable probing.
        /// </summary>
        int AutoDetectProviderTimeout { get; }
    }

    public class ProxyConfiguration
    {
        public ProxyConfiguration(
            Uri proxyAddress,
            string userName = null,
            string password = null,
            string noProxyRaw = null,
            bool isDeprecatedSource = false)
        {
            Address = proxyAddress;
            UserName = userName;
            Password = password;
            NoProxyRaw = noProxyRaw;
            IsDeprecatedSource = isDeprecatedSource;
        }

        /// <summary>
        /// True if the proxy configuration method is deprecated, false otherwise.
        /// </summary>
        public bool IsDeprecatedSource { get; }

        /// <summary>
        /// Configured proxy URI (proxy server address and optional user authentication information).
        /// </summary>
        public Uri Address { get; }

        /// <summary>
        /// User name to use to authenticate to the proxy address.
        /// </summary>
        public string UserName { get; }

        /// <summary>
        /// Password to use to authenticate to the proxy address.
        /// </summary>
        public string Password { get; }

        /// <summary>
        /// List of host names that should not be proxied.
        /// </summary>
        /// <remarks>
        /// This is the raw value from the NO_PROXY setting. Values are expected to be in a libcurl compatible format.
        /// <para/>
        /// To convert the string in to a set of .NET regular expressions for use with proxy settings,
        /// use the <see cref="ConvertToBypassRegexArray"/> method.
        /// </remarks>
        public string NoProxyRaw { get; }

        /// <summary>
        /// Convert a libcurl-format NO_PROXY string in to a set of equivalent .NET regular expressions.
        /// </summary>
        /// <param name="noProxy">NO_PROXY value in a libcurl-compatible format.</param>
        /// <returns>Array of regular expressions.</returns>
        public static IEnumerable<string> ConvertToBypassRegexArray(string noProxy)
        {
            if (string.IsNullOrWhiteSpace(noProxy))
            {
                yield break;
            }

            string[] split = noProxy.Split(new[] { ",", " " }, StringSplitOptions.RemoveEmptyEntries);

            var normalized = new StringBuilder();
            var regex = new StringBuilder();
            foreach (string str in split)
            {
                // Normalize the domain search value
                normalized.Clear();
                normalized.Append(str);

                // Strip leading subdomain wildcards: *.example.com => example.com
                if (normalized.Length > 1 && normalized[0] == '*' && normalized[1] == '.')
                {
                    normalized.Remove(0, 2);
                }

                // Strip all leading dots: .example.com => example.com
                while (normalized.Length > 0 && normalized[0] == '.')
                {
                    normalized.Remove(0, 1);
                }

                // Build the regular expression
                regex.Clear();

                // Only match (sub-)domains, not partial domain names.
                // For example: "example.com" should match "http://example.com" and
                // "http://www.example.com" but not "http://notanexample.com".
                regex.Append(@"(\.|\:\/\/)");

                // Add the escaped domain search value
                regex.Append(Regex.Escape(normalized.ToString()));

                // Ensure we only match the specified port and TLD
                regex.Append('$');

                yield return regex.ToString();
            }
        }
    }

    public enum TlsBackend
    {
        OpenSsl,
        Schannel,
        Other,
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
            IEnumerable<string> allValues = GetSettingValues(envarName, section, property, false);

            value = allValues.FirstOrDefault();

            return value != null;
        }

        public bool TryGetPathSetting(string envarName, string section, string property, out string value)
        {
            IEnumerable<string> allValues = GetSettingValues(envarName, section, property, true);

            value = allValues.FirstOrDefault();

            return value != null;
        }

        public IEnumerable<string> GetSettingValues(string envarName, string section, string property, bool isPath)
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
                     * It is also important to note that although the section and property names are NOT case
                     * sensitive, the "scope" part IS case sensitive! We must be careful when searching to ensure
                     * we follow Git's rules.
                     *
                     */

                    // Enumerate all configuration entries with the correct section and property name
                    // and make a local copy of them here to avoid needing to call `TryGetValue` on the
                    // IGitConfiguration object multiple times in a loop below.
                    var configEntries = new Dictionary<string, string>(GitConfigurationKeyComparer.Instance);
                    config.Enumerate(section, property, entry =>
                    {
                        configEntries[entry.Key] = entry.Value;

                        // Continue the enumeration
                        return true;
                    });

                    foreach (string scope in RemoteUri.GetGitConfigurationScopes())
                    {
                        string queryName = $"{section}.{scope}.{property}";
                        // Look for a scoped entry that includes the scheme "protocol://example.com" first as
                        // this is more specific. If `isPath` is true, then re-get the value from the
                        // `GitConfiguration` with `isPath` specified.
                        if (configEntries.TryGetValue(queryName, out value) &&
                            (!isPath || config.TryGet(queryName, isPath, out value)))
                        {
                            yield return value;
                        }

                        // Now look for a scoped entry that omits the scheme "example.com" second as this is less
                        // specific. As above, if `isPath` is true, get the configuration setting again with
                        // `isPath` specified.
                        string scopeWithoutScheme = scope.TrimUntilIndexOf(Uri.SchemeDelimiter);
                        string queryWithSchemeName = $"{section}.{scopeWithoutScheme}.{property}";
                        if (configEntries.TryGetValue(queryWithSchemeName, out value) &&
                            (!isPath || config.TryGet(queryWithSchemeName, isPath, out value)))
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
                if (config.TryGet($"{section}.{property}", isPath, out value))
                {
                    yield return value;
                }

                // Check for an externally specified default value
                if (TryGetExternalDefault(section, property, out string defaultValue))
                {
                    yield return defaultValue;
                }
            }
        }

        /// <summary>
        /// Try to get the default value for a configuration setting.
        /// This may come from external policies or the Operating System.
        /// </summary>
        /// <param name="section">Configuration section name.</param>
        /// <param name="property">Configuration property name.</param>
        /// <param name="value">Value of the configuration setting, or null.</param>
        /// <returns>True if a default setting has been set, false otherwise.</returns>
        protected virtual bool TryGetExternalDefault(string section, string property, out string value)
        {
            value = null;
            return false;
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
                     * -------------------------------------------------------------
                     * | Value(s) | Old meaning               | New meaning        |
                     * |-----------------------------------------------------------|
                     * | auto     | Prompt if required        | [unchanged]        |
                     * |-----------------------------------------------------------|
                     * | never    | Never prompt ─ fail if    | [unchanged]        |
                     * | false    | interaction is required   |                    |
                     * |-----------------------------------------------------------|
                     * | always   | Always prompt ─ don't use | Prompt if required |
                     * | force    | cached credentials        |                    |
                     * | true     |                           |                    |
                     * -------------------------------------------------------------
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

        public string CustomCertificateBundlePath =>
            TryGetPathSetting(KnownEnvars.GitSslCaInfo, KnownGitCfg.Http.SectionName, KnownGitCfg.Http.SslCaInfo, out string value) ? value : null;

        public TlsBackend TlsBackend =>
            TryGetSetting(null, KnownGitCfg.Http.SectionName, KnownGitCfg.Http.SslBackend, out string config)
                ? (Enum.TryParse(config, true, out TlsBackend backend) ? backend : GitCredentialManager.TlsBackend.Other)
                : default(TlsBackend);

        public bool UseCustomCertificateBundleWithSchannel =>
            TryGetSetting(null, KnownGitCfg.Http.SectionName, KnownGitCfg.Http.SchannelUseSslCaInfo, out string schannelUseSslCaInfo) &&
                schannelUseSslCaInfo.ToBooleanyOrDefault(false);

        public int AutoDetectProviderTimeout
        {
            get
            {
                if (TryGetSetting(KnownEnvars.GcmAutoDetectTimeout,
                        KnownGitCfg.Credential.SectionName,
                        KnownGitCfg.Credential.AutoDetectTimeout,
                        out string valueStr) &&
                    ConvertUtils.TryToInt32(valueStr, out int value))
                {
                    return value;
                }

                return Constants.DefaultAutoDetectProviderTimeoutMs;
            }
        }

        public ProxyConfiguration GetProxyConfiguration()
        {
            bool TryGetUriSetting(string envarName, string section, string property, out Uri uri)
            {
                IEnumerable<string> allValues = GetSettingValues(envarName, section, property, false);

                foreach (var value in allValues)
                {
                    if (Uri.TryCreate(value, UriKind.Absolute, out uri))
                    {
                        return true;
                    }
                    else if (string.IsNullOrWhiteSpace(value))
                    {
                        // An empty string value means "no proxy"
                        return false;
                    }
                }

                uri = null;
                return false;
            }

            char[] bypassListSeparators = {',', ' '};

            ProxyConfiguration CreateConfiguration(Uri uri, bool isLegacy = false)
            {
                // Strip the userinfo, query, and fragment parts of the Uri retaining only the scheme, host, port, and path
                Uri address = new UriBuilder(uri)
                {
                    UserName = string.Empty,
                    Password = string.Empty,
                    Query    = string.Empty,
                    Fragment = string.Empty,
                }.Uri;

                // Extract the username and password from the URI if present
                uri.TryGetUserInfo(out string userName, out string password);

                // Get the proxy bypass host names
                // Check both lowercase "no_proxy" and uppercase "NO_PROXY" variants (preferring the former)
                if (!_environment.Variables.TryGetValue(KnownEnvars.CurlNoProxy, out string noProxyStr))
                {
                    _environment.Variables.TryGetValue(KnownEnvars.CurlNoProxyUpper, out noProxyStr);
                }

                return new ProxyConfiguration(address, userName, password, noProxyStr, isLegacy);
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
             *        https_proxy
             *        HTTPS_PROXY
             *        http_proxy (note that uppercase HTTP_PROXY is not supported by libcurl)
             *        all_proxy
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
             * We also support the cURL "no_proxy" / "NO_PROXY" environment variables in conjunction with any
             * of the above supported proxy address configurations. This comma separated list of
             * host names (or host name wildcards) should be respected and the proxy should NOT
             * be used for these addresses.
             *
             */

            bool isHttps = StringComparer.OrdinalIgnoreCase.Equals(Uri.UriSchemeHttps, RemoteUri?.Scheme);

            Uri proxyUri;

            // 1. GCM proxy Git configuration (deprecated)
            if (isHttps && TryGetUriSetting(null, GitCredCfg.SectionName, GitCredCfg.HttpsProxy, out proxyUri) ||
                TryGetUriSetting(null, GitCredCfg.SectionName, GitCredCfg.HttpProxy, out proxyUri))
            {
                return CreateConfiguration(proxyUri, isLegacy: true);
            }

            // 2. Standard Git configuration
            if (TryGetUriSetting(null, GitHttpCfg.SectionName, GitHttpCfg.Proxy, out proxyUri))
            {
                return CreateConfiguration(proxyUri);
            }

            // 3. cURL environment variables (both lower- and uppercase variants)
            // Prefer the lowercase variants as these are quasi-standard.
            if (isHttps && TryGetUriSetting(KnownEnvars.CurlHttpsProxy, null, null, out proxyUri) ||
                isHttps && TryGetUriSetting(KnownEnvars.CurlHttpsProxyUpper, null, null, out proxyUri) ||
                TryGetUriSetting(KnownEnvars.CurlHttpProxy, null, null, out proxyUri) ||
                // Note that the uppercase HTTP_PROXY is not recognized by libcurl
                TryGetUriSetting(KnownEnvars.CurlAllProxy, null, null, out proxyUri) ||
                TryGetUriSetting(KnownEnvars.CurlAllProxyUpper, null, null, out proxyUri))
            {
                return CreateConfiguration(proxyUri);
            }

            // 4. GCM proxy environment variable (deprecated)
            if (TryGetUriSetting(KnownEnvars.GcmHttpProxy, null, null, out proxyUri))
            {
                return CreateConfiguration(proxyUri, isLegacy: true);
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
