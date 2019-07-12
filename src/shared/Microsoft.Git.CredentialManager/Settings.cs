// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;
using KnownEnvars = Microsoft.Git.CredentialManager.Constants.EnvironmentVariables;
using KnownGitCfg = Microsoft.Git.CredentialManager.Constants.GitConfiguration;

namespace Microsoft.Git.CredentialManager
{
    /// <summary>
    /// Component that represents settings for Git Credential Manager as found from the environment and Git configuration.
    /// </summary>
    public interface ISettings
    {
        /// <summary>
        /// True if debugging is enabled, false otherwise.
        /// </summary>
        bool IsDebuggingEnabled { get; }

        /// <summary>
        /// True if terminal prompting is enabled, false otherwise.
        /// </summary>
        bool IsTerminalPromptsEnabled { get; }

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
    }

    public class Settings : ISettings
    {
        private readonly IEnvironmentVariables _environment;
        private readonly IGit _git;

        public Settings(IEnvironmentVariables environmentVariables, IGit git)
        {
            _environment = environmentVariables;
            _git = git;
        }

        public bool IsDebuggingEnabled => _environment.GetBooleanyOrDefault(KnownEnvars.GcmDebug, false);

        public bool IsTerminalPromptsEnabled => _environment.GetBooleanyOrDefault(KnownEnvars.GitTerminalPrompts, true);

        public bool GetTracingEnabled(out string value) => _environment.TryGetValue(KnownEnvars.GcmTrace, out value) && !value.IsFalsey();

        public bool IsSecretTracingEnabled => _environment.GetBooleanyOrDefault(KnownEnvars.GcmTraceSecrets, false);

        /// <summary>
        /// Try and get the value of a specified setting as specified in the environment and Git configuration,
        /// with the environment taking precedence over Git.
        /// </summary>
        /// <param name="repositoryPath">Optional path of a repository to lookup local configuration from.</param>
        /// <param name="remoteUri">Optional git remote address that settings should be scoped to.</param>
        /// <param name="envarName">Optional environment variable name.</param>
        /// <param name="section">Optional Git configuration section name.</param>
        /// <param name="property">Git configuration property name. Required if <paramref name="section"/> is set, optional otherwise.</param>
        /// <param name="value">Value of the requested setting.</param>
        /// <returns>True if a setting value was found, false otherwise.</returns>
        public bool TryGetSetting(string repositoryPath, Uri remoteUri, string envarName, string section, string property, out string value)
        {
            IEnumerable<string> allValues = GetSettingValues(repositoryPath, remoteUri, envarName, section, property);

            value = allValues.FirstOrDefault();

            return value != null;
        }

        /// <summary>
        /// Try and get the all values of a specified setting as specified in the environment and Git configuration,
        /// in the correct order or precedence.
        /// </summary>
        /// <param name="repositoryPath">Optional path of a repository to lookup local configuration from.</param>
        /// <param name="remoteUri">Optional git remote address that settings should be scoped to.</param>
        /// <param name="envarName">Optional environment variable name.</param>
        /// <param name="section">Optional Git configuration section name.</param>
        /// <param name="property">Git configuration property name. Required if <paramref name="section"/> is set, optional otherwise.</param>
        /// <returns>All values for the specified setting, in order of precedence, or an empty collection if no such values are set.</returns>
        public IEnumerable<string> GetSettingValues(string repositoryPath, Uri remoteUri, string envarName, string section, string property)
        {
            string value;

            if (envarName != null)
            {
                if (_environment.TryGetValue(envarName, out value))
                {
                    yield return value;
                }
            }

            if (section != null && property != null)
            {
                using (var config = _git.GetConfiguration(repositoryPath))
                {
                    if (remoteUri != null)
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
                        foreach (string scope in remoteUri.GetGitConfigurationScopes())
                        {
                            // Look for a scoped entry that includes the scheme "protocol://example.com" first as this is more specific
                            if (config.TryGetValue(section, scope, property, out value))
                            {
                                yield return value;
                            }

                            // Now look for a scoped entry that omits the scheme "example.com" second as this is less specific
                            string scopeWithoutScheme = scope.TrimUntilIndexOf(Uri.SchemeDelimiter);
                            if (config.TryGetValue(section, scopeWithoutScheme, property, out value))
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
                    if (config.TryGetValue(section, property, out value))
                    {
                        yield return value;
                    }
                }
            }
        }
    }
}
