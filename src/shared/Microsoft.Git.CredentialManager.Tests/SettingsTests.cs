// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Git.CredentialManager.Tests.Objects;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests
{
    public class SettingsTests
    {
        [Fact]
        public void Settings_IsDebuggingEnabled_EnvarUnset_ReturnsFalse()
        {
            var envars = new TestEnvironment();
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.False(settings.IsDebuggingEnabled);
        }

        [Fact]
        public void Settings_IsDebuggingEnabled_EnvarTruthy_ReturnsTrue()
        {
            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GcmDebug] = "1"}
            };
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.True(settings.IsDebuggingEnabled);
        }

        [Fact]
        public void Settings_IsDebuggingEnabled_EnvarFalsey_ReturnsFalse()
        {
            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GcmDebug] = "0"}
            };
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.False(settings.IsDebuggingEnabled);
        }

        [Fact]
        public void Settings_IsTerminalPromptsEnabled_EnvarUnset_ReturnsTrue()
        {
            var envars = new TestEnvironment();
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.True(settings.IsTerminalPromptsEnabled);
        }

        [Fact]
        public void Settings_IsTerminalPromptsEnabled_EnvarTruthy_ReturnsTrue()
        {
            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GitTerminalPrompts] = "1"}
            };
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.True(settings.IsTerminalPromptsEnabled);
        }

        [Fact]
        public void Settings_IsTerminalPromptsEnabled_EnvarFalsey_ReturnsFalse()
        {
            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GitTerminalPrompts] = "0"}
            };
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.False(settings.IsTerminalPromptsEnabled);
        }

        [Fact]
        public void Settings_IsInteractionAllowed_EnvarUnset_ReturnsTrue()
        {
            var envars = new TestEnvironment();
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.True(settings.IsInteractionAllowed);
        }

        [Fact]
        public void Settings_IsInteractionAllowed_EnvarTruthy_ReturnsTrue()
        {
            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GcmInteractive] = "1"}
            };
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.True(settings.IsInteractionAllowed);
        }

        [Fact]
        public void Settings_IsInteractionAllowed_EnvarFalsey_ReturnsFalse()
        {
            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GcmInteractive] = "0"},
            };
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.False(settings.IsInteractionAllowed);
        }

        [Fact]
        public void Settings_IsInteractionAllowed_ConfigAuto_ReturnsTrue()
        {
            const string section = Constants.GitConfiguration.Credential.SectionName;
            const string property = Constants.GitConfiguration.Credential.Interactive;

            var envars = new TestEnvironment();
            var git = new TestGit();
            git.GlobalConfiguration[$"{section}.{property}"] = "auto";

            var settings = new Settings(envars, git);

            Assert.True(settings.IsInteractionAllowed);
        }

        [Fact]
        public void Settings_IsInteractionAllowed_ConfigAlways_ReturnsTrue()
        {
            const string section = Constants.GitConfiguration.Credential.SectionName;
            const string property = Constants.GitConfiguration.Credential.Interactive;

            var envars = new TestEnvironment();
            var git = new TestGit();
            git.GlobalConfiguration[$"{section}.{property}"] = "always";

            var settings = new Settings(envars, git);

            Assert.True(settings.IsInteractionAllowed);
        }

        [Fact]
        public void Settings_IsInteractionAllowed_ConfigNever_ReturnsFalse()
        {
            const string section = Constants.GitConfiguration.Credential.SectionName;
            const string property = Constants.GitConfiguration.Credential.Interactive;

            var envars = new TestEnvironment();
            var git = new TestGit();
            git.GlobalConfiguration[$"{section}.{property}"] = "never";

            var settings = new Settings(envars, git);

            Assert.False(settings.IsInteractionAllowed);
        }

        [Fact]
        public void Settings_IsInteractionAllowed_ConfigTruthy_ReturnsTrue()
        {
            const string section = Constants.GitConfiguration.Credential.SectionName;
            const string property = Constants.GitConfiguration.Credential.Interactive;

            var envars = new TestEnvironment();
            var git = new TestGit();
            git.GlobalConfiguration[$"{section}.{property}"] = "1";

            var settings = new Settings(envars, git);

            Assert.True(settings.IsInteractionAllowed);
        }

        [Fact]
        public void Settings_IsInteractionAllowed_ConfigFalsey_ReturnsFalse()
        {
            const string section = Constants.GitConfiguration.Credential.SectionName;
            const string property = Constants.GitConfiguration.Credential.Interactive;

            var envars = new TestEnvironment();
            var git = new TestGit();
            git.GlobalConfiguration[$"{section}.{property}"] = "0";

            var settings = new Settings(envars, git);

            Assert.False(settings.IsInteractionAllowed);
        }

        [Fact]
        public void Settings_IsInteractionAllowed_ConfigNonBooleanyValue_ReturnsTrue()
        {
            const string section = Constants.GitConfiguration.Credential.SectionName;
            const string property = Constants.GitConfiguration.Credential.Interactive;

            var envars = new TestEnvironment();
            var git = new TestGit();
            git.GlobalConfiguration[$"{section}.{property}"] = Guid.NewGuid().ToString();

            var settings = new Settings(envars, git);

            Assert.True(settings.IsInteractionAllowed);
        }

        [Fact]
        public void Settings_IsTracingEnabled_EnvarUnset_ReturnsFalse()
        {
            var envars = new TestEnvironment();
            var git = new TestGit();

            var settings = new Settings(envars, git);
            var result = settings.GetTracingEnabled(out string actualValue);

            Assert.False(result);
        }

        [Fact]
        public void Settings_IsTracingEnabled_EnvarTruthy_ReturnsTrueOutValue()
        {
            const string expectedValue = "1";
            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GcmTrace] = expectedValue}
            };
            var git = new TestGit();

            var settings = new Settings(envars, git);
            var result = settings.GetTracingEnabled(out string actualValue);

            Assert.True(result);
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Settings_IsTracingEnabled_EnvarFalsey_ReturnsFalseOutValue()
        {
            const string expectedValue = "0";
            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GcmTrace] = expectedValue}
            };
            var git = new TestGit();

            var settings = new Settings(envars, git);
            var result = settings.GetTracingEnabled(out string actualValue);

            Assert.False(result);
            Assert.Equal(expectedValue, actualValue);
        }


        [Fact]
        public void Settings_IsTracingEnabled_EnvarPathy_ReturnsTrueOutValue()
        {
            const string expectedValue = "/tmp/gcm.log";
            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GcmTrace] = expectedValue}
            };
            var git = new TestGit();

            var settings = new Settings(envars, git);
            var result = settings.GetTracingEnabled(out string actualValue);

            Assert.True(result);
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Settings_IsSecretTracingEnabled_EnvarUnset_ReturnsFalse()
        {
            var envars = new TestEnvironment();
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.False(settings.IsSecretTracingEnabled);
        }

        [Fact]
        public void Settings_IsSecretTracingEnabled_EnvarTruthy_ReturnsTrue()
        {
            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GcmTraceSecrets] = "1"}
            };
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.True(settings.IsSecretTracingEnabled);
        }

        [Fact]
        public void Settings_IsSecretTracingEnabled_EnvarFalsey_ReturnsFalse()
        {
            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GcmTraceSecrets] = "0"}
            };
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.False(settings.IsSecretTracingEnabled);
        }

        [Fact]
        public void Settings_IsWindowsIntegratedAuthenticationEnabled_EnvarUnset_ReturnsTrue()
        {
            var envars = new TestEnvironment();
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.True(settings.IsWindowsIntegratedAuthenticationEnabled);
        }

        [Fact]
        public void Settings_IsWindowsIntegratedAuthenticationEnabled_EnvarTruthy_ReturnsTrue()
        {
            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GcmAllowWia] = "1"}
            };
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.True(settings.IsWindowsIntegratedAuthenticationEnabled);
        }

        [Fact]
        public void Settings_IsWindowsIntegratedAuthenticationEnabled_EnvarFalsey_ReturnsFalse()
        {
            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GcmAllowWia] = "0"},
            };
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.False(settings.IsWindowsIntegratedAuthenticationEnabled);
        }

        [Fact]
        public void Settings_IsWindowsIntegratedAuthenticationEnabled_EnvarNonBooleanyValue_ReturnsTrue()
        {
            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GcmAllowWia] = Guid.NewGuid().ToString("N")},
            };
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.True(settings.IsWindowsIntegratedAuthenticationEnabled);
        }

        [Fact]
        public void Settings_IsWindowsIntegratedAuthenticationEnabled_ConfigUnset_ReturnsTrue()
        {
            var envars = new TestEnvironment();
            var git = new TestGit();

            var settings = new Settings(envars, git);

            Assert.True(settings.IsWindowsIntegratedAuthenticationEnabled);
        }

        [Fact]
        public void Settings_IsWindowsIntegratedAuthenticationEnabled_ConfigTruthy_ReturnsTrue()
        {
            const string section = Constants.GitConfiguration.Credential.SectionName;
            const string property = Constants.GitConfiguration.Credential.AllowWia;

            var envars = new TestEnvironment();
            var git = new TestGit();
            git.GlobalConfiguration[$"{section}.{property}"] = "1";

            var settings = new Settings(envars, git);

            Assert.True(settings.IsWindowsIntegratedAuthenticationEnabled);
        }

        [Fact]
        public void Settings_IsWindowsIntegratedAuthenticationEnabled_ConfigFalsey_ReturnsFalse()
        {
            const string section = Constants.GitConfiguration.Credential.SectionName;
            const string property = Constants.GitConfiguration.Credential.AllowWia;

            var envars = new TestEnvironment();
            var git = new TestGit();
            git.GlobalConfiguration[$"{section}.{property}"] = "0";

            var settings = new Settings(envars, git);

            Assert.False(settings.IsWindowsIntegratedAuthenticationEnabled);
        }

        [Fact]
        public void Settings_IsWindowsIntegratedAuthenticationEnabled_ConfigNonBooleanyValue_ReturnsTrue()
        {
            const string section = Constants.GitConfiguration.Credential.SectionName;
            const string property = Constants.GitConfiguration.Credential.AllowWia;

            var envars = new TestEnvironment();
            var git = new TestGit();
            git.GlobalConfiguration[$"{section}.{property}"] = Guid.NewGuid().ToString();

            var settings = new Settings(envars, git);

            Assert.True(settings.IsWindowsIntegratedAuthenticationEnabled);
        }

        [Fact]
        public void Settings_ProxyConfiguration_Unset_ReturnsNull()
        {
            const string remoteUrl = "http://example.com/foo.git";
            var remoteUri = new Uri(remoteUrl);

            var envars = new TestEnvironment();
            var git = new TestGit();

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };

            ProxyConfiguration config = settings.GetProxyConfiguration();

            Assert.Null(config);
        }

        [Fact]
        public void Settings_ProxyConfiguration_GcmHttpConfig_ReturnsValue()
        {
            const string remoteUrl = "http://example.com/foo.git";
            const string section = Constants.GitConfiguration.Credential.SectionName;
            const string property = Constants.GitConfiguration.Credential.HttpProxy;
            var remoteUri = new Uri(remoteUrl);

            const string expectedUserName = "john.doe";
            const string expectedPassword = "letmein123";
            var expectedAddress = new Uri("http://proxy.example.com");
            var settingValue = new Uri("http://john.doe:letmein123@proxy.example.com");
            var bypassList = new List<string> {"contoso.com", "fabrikam.com"};

            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.CurlNoProxy] = string.Join(',', bypassList)}
            };
            var git = new TestGit();
            git.GlobalConfiguration[$"{section}.{property}"] = settingValue.ToString();

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };

            ProxyConfiguration actualConfig = settings.GetProxyConfiguration();

            Assert.NotNull(actualConfig);
            Assert.Equal(expectedAddress, actualConfig.Address);
            Assert.Equal(expectedUserName, actualConfig.UserName);
            Assert.Equal(expectedPassword, actualConfig.Password);
            Assert.Equal(bypassList, actualConfig.BypassHosts);
            Assert.True(actualConfig.IsDeprecatedSource);
        }

        [Fact]
        public void Settings_ProxyConfiguration_GcmHttpsConfig_ReturnsValue()
        {
            const string remoteUrl = "https://example.com/foo.git";
            const string section = Constants.GitConfiguration.Credential.SectionName;
            const string property = Constants.GitConfiguration.Credential.HttpsProxy;
            var remoteUri = new Uri(remoteUrl);

            const string expectedUserName = "john.doe";
            const string expectedPassword = "letmein123";
            var expectedAddress = new Uri("http://proxy.example.com");
            var settingValue = new Uri("http://john.doe:letmein123@proxy.example.com");
            var bypassList = new List<string> {"contoso.com", "fabrikam.com"};

            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.CurlNoProxy] = string.Join(',', bypassList)}
            };
            var git = new TestGit();
            git.GlobalConfiguration[$"{section}.{property}"] = settingValue.ToString();

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };

            ProxyConfiguration actualConfig = settings.GetProxyConfiguration();

            Assert.NotNull(actualConfig);
            Assert.Equal(expectedAddress, actualConfig.Address);
            Assert.Equal(expectedUserName, actualConfig.UserName);
            Assert.Equal(expectedPassword, actualConfig.Password);
            Assert.Equal(bypassList, actualConfig.BypassHosts);
            Assert.True(actualConfig.IsDeprecatedSource);
        }

        [Fact]
        public void Settings_ProxyConfiguration_GitHttpConfig_ReturnsValue()
        {
            const string remoteUrl = "http://example.com/foo.git";
            const string section = Constants.GitConfiguration.Http.SectionName;
            const string property = Constants.GitConfiguration.Http.Proxy;
            var remoteUri = new Uri(remoteUrl);

            const string expectedUserName = "john.doe";
            const string expectedPassword = "letmein123";
            var expectedAddress = new Uri("http://proxy.example.com");
            var settingValue = new Uri("http://john.doe:letmein123@proxy.example.com");
            var bypassList = new List<string> {"contoso.com", "fabrikam.com"};

            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.CurlNoProxy] = string.Join(',', bypassList)}
            };
            var git = new TestGit();
            git.GlobalConfiguration[$"{section}.{property}"] = settingValue.ToString();

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };

            ProxyConfiguration actualConfig = settings.GetProxyConfiguration();

            Assert.NotNull(actualConfig);
            Assert.Equal(expectedAddress, actualConfig.Address);
            Assert.Equal(expectedUserName, actualConfig.UserName);
            Assert.Equal(expectedPassword, actualConfig.Password);
            Assert.Equal(bypassList, actualConfig.BypassHosts);
            Assert.False(actualConfig.IsDeprecatedSource);
        }

        [Fact]
        public void Settings_ProxyConfiguration_CurlHttpEnvar_ReturnsValue()
        {
            const string remoteUrl = "http://example.com/foo.git";
            var remoteUri = new Uri(remoteUrl);

            const string expectedUserName = "john.doe";
            const string expectedPassword = "letmein123";
            var expectedAddress = new Uri("http://proxy.example.com");
            var settingValue = new Uri("http://john.doe:letmein123@proxy.example.com");
            var bypassList = new List<string> {"contoso.com", "fabrikam.com"};

            var envars = new TestEnvironment
            {
                Variables =
                {
                    [Constants.EnvironmentVariables.CurlHttpProxy] = settingValue.ToString(),
                    [Constants.EnvironmentVariables.CurlNoProxy] = string.Join(',', bypassList)
                }
            };
            var git = new TestGit();

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };

            ProxyConfiguration actualConfig = settings.GetProxyConfiguration();

            Assert.NotNull(actualConfig);
            Assert.Equal(expectedAddress, actualConfig.Address);
            Assert.Equal(expectedUserName, actualConfig.UserName);
            Assert.Equal(expectedPassword, actualConfig.Password);
            Assert.Equal(bypassList, actualConfig.BypassHosts);
            Assert.False(actualConfig.IsDeprecatedSource);
        }

        [Fact]
        public void Settings_ProxyConfiguration_CurlHttpsEnvar_ReturnsValue()
        {
            const string remoteUrl = "https://example.com/foo.git";
            var remoteUri = new Uri(remoteUrl);

            const string expectedUserName = "john.doe";
            const string expectedPassword = "letmein123";
            var expectedAddress = new Uri("http://proxy.example.com");
            var settingValue = new Uri("http://john.doe:letmein123@proxy.example.com");
            var bypassList = new List<string> {"contoso.com", "fabrikam.com"};

            var envars = new TestEnvironment
            {
                Variables =
                {
                    [Constants.EnvironmentVariables.CurlHttpsProxy] = settingValue.ToString(),
                    [Constants.EnvironmentVariables.CurlNoProxy] = string.Join(',', bypassList)
                }
            };
            var git = new TestGit();

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };

            ProxyConfiguration actualConfig = settings.GetProxyConfiguration();

            Assert.NotNull(actualConfig);
            Assert.Equal(expectedAddress, actualConfig.Address);
            Assert.Equal(expectedUserName, actualConfig.UserName);
            Assert.Equal(expectedPassword, actualConfig.Password);
            Assert.Equal(bypassList, actualConfig.BypassHosts);
            Assert.False(actualConfig.IsDeprecatedSource);
        }

        [Fact]
        public void Settings_TryGetProxy_CurlAllEnvar_ReturnsValue()
        {
            const string remoteUrl = "https://example.com/foo.git";
            var remoteUri = new Uri(remoteUrl);

            const string expectedUserName = "john.doe";
            const string expectedPassword = "letmein123";
            var expectedAddress = new Uri("http://proxy.example.com");
            var settingValue = new Uri("http://john.doe:letmein123@proxy.example.com");
            var bypassList = new List<string> {"contoso.com", "fabrikam.com"};

            var envars = new TestEnvironment
            {
                Variables =
                {
                    [Constants.EnvironmentVariables.CurlAllProxy] = settingValue.ToString(),
                    [Constants.EnvironmentVariables.CurlNoProxy] = string.Join(',', bypassList)
                }
            };
            var git = new TestGit();

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };

            ProxyConfiguration actualConfig = settings.GetProxyConfiguration();

            Assert.NotNull(actualConfig);
            Assert.Equal(expectedAddress, actualConfig.Address);
            Assert.Equal(expectedUserName, actualConfig.UserName);
            Assert.Equal(expectedPassword, actualConfig.Password);
            Assert.Equal(bypassList, actualConfig.BypassHosts);
            Assert.False(actualConfig.IsDeprecatedSource);
        }

        [Fact]
        public void Settings_ProxyConfiguration_LegacyGcmEnvar_ReturnsValue()
        {
            const string remoteUrl = "http://example.com/foo.git";
            var remoteUri = new Uri(remoteUrl);

            const string expectedUserName = "john.doe";
            const string expectedPassword = "letmein123";
            var expectedAddress = new Uri("http://proxy.example.com");
            var settingValue = new Uri("http://john.doe:letmein123@proxy.example.com");
            var bypassList = new List<string> {"https://contoso.com", ".*fabrikam\\.com"};

            var envars = new TestEnvironment
            {
                Variables =
                {
                    [Constants.EnvironmentVariables.GcmHttpProxy] = settingValue.ToString(),
                    [Constants.EnvironmentVariables.CurlNoProxy] = string.Join(',', bypassList)
                }
            };
            var git = new TestGit();

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };

            ProxyConfiguration actualConfig = settings.GetProxyConfiguration();

            Assert.NotNull(actualConfig);
            Assert.Equal(expectedAddress, actualConfig.Address);
            Assert.Equal(expectedUserName, actualConfig.UserName);
            Assert.Equal(expectedPassword, actualConfig.Password);
            Assert.Equal(bypassList, actualConfig.BypassHosts);
            Assert.True(actualConfig.IsDeprecatedSource);
        }

        [Fact]
        public void Settings_ProxyConfiguration_Precedence_ReturnsValue()
        {
            // 1. GCM proxy Git configuration (deprecated)
            //      credential.httpsProxy
            //      credential.httpProxy
            // 2. Standard Git configuration
            //      http.proxy
            // 3. cURL environment variables
            //      HTTPS_PROXY
            //      HTTP_PROXY
            //      ALL_PROXY
            // 4. GCM proxy environment variable (deprecated)
            //      GCM_HTTP_PROXY

            const string remoteUrl = "http://example.com/foo.git";
            var remoteUri = new Uri(remoteUrl);

            var value1 = new Uri("http://proxy1.example.com");
            var value2 = new Uri("http://proxy2.example.com");
            var value3 = new Uri("http://proxy3.example.com");
            var value4 = new Uri("http://proxy4.example.com");

            var envars = new TestEnvironment();
            var git = new TestGit();

            void RunTest(Uri expectedValue)
            {
                var settings = new Settings(envars, git)
                {
                    RemoteUri = remoteUri
                };
                ProxyConfiguration actualConfig = settings.GetProxyConfiguration();
                Assert.Equal(expectedValue, actualConfig.Address);
            }

             // Test case 1: cURL environment variables > GCM_HTTP_PROXY
            envars.Variables[Constants.EnvironmentVariables.GcmHttpProxy] = value1.ToString();
            envars.Variables[Constants.EnvironmentVariables.CurlHttpProxy] = value2.ToString();
            RunTest(value2);

             // Test case 2: http.proxy > cURL environment variables
            git.GlobalConfiguration[$"{Constants.GitConfiguration.Http.SectionName}.{Constants.GitConfiguration.Http.Proxy}"] = value3.ToString();
            RunTest(value3);

             // Test case 3: credential.httpProxy > http.proxy
             git.GlobalConfiguration[$"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.HttpProxy}"] = value4.ToString();
            RunTest(value4);
        }

        [Fact]
        public void Settings_ProviderOverride_Unset_ReturnsNull()
        {
            const string remoteUrl = "http://example.com/foo.git";
            var remoteUri = new Uri(remoteUrl);

            var envars = new TestEnvironment();
            var git = new TestGit();

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };
            string value = settings.ProviderOverride;

            Assert.Null(value);
        }

        [Fact]
        public void Settings_ProviderOverride_EnvarSet_ReturnsValue()
        {
            const string remoteUrl = "http://example.com/foo.git";
            var remoteUri = new Uri(remoteUrl);

            const string expectedValue = "provider1";

            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GcmProvider] = expectedValue}
            };
            var git = new TestGit();

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };
            string actualValue = settings.ProviderOverride;

            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Settings_ProviderOverride_ConfigSet_ReturnsValue()
        {
            const string remoteUrl = "http://example.com/foo.git";
            const string section = Constants.GitConfiguration.Credential.SectionName;
            const string property = Constants.GitConfiguration.Credential.Provider;
            var remoteUri = new Uri(remoteUrl);

            const string expectedValue = "provider1";

            var envars = new TestEnvironment();
            var git = new TestGit();
            git.GlobalConfiguration[$"{section}.{property}"] = expectedValue;

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };
            string actualValue = settings.ProviderOverride;

            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Settings_ProviderOverride_EnvarAndConfigSet_ReturnsEnvarValue()
        {
            const string remoteUrl = "http://example.com/foo.git";
            const string section = Constants.GitConfiguration.Credential.SectionName;
            const string property = Constants.GitConfiguration.Credential.Provider;
            var remoteUri = new Uri(remoteUrl);

            const string expectedValue = "provider1";
            const string otherValue = "provider2";

            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GcmProvider] = expectedValue}
            };
            var git = new TestGit();
            git.GlobalConfiguration[$"{section}.{property}"] = otherValue;

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };
            string actualValue = settings.ProviderOverride;

            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Settings_LegacyAuthorityOverride_Unset_ReturnsNull()
        {
            const string remoteUrl = "http://example.com/foo.git";
            var remoteUri = new Uri(remoteUrl);

            var envars = new TestEnvironment();
            var git = new TestGit();

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };
            string value = settings.LegacyAuthorityOverride;

            Assert.Null(value);
        }

        [Fact]
        public void Settings_LegacyAuthorityOverride_EnvarSet_ReturnsValue()
        {
            const string remoteUrl = "http://example.com/foo.git";
            var remoteUri = new Uri(remoteUrl);

            const string expectedValue = "provider1";

            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GcmAuthority] = expectedValue}
            };
            var git = new TestGit();

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };
            string actualValue = settings.LegacyAuthorityOverride;

            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Settings_LegacyAuthorityOverride_ConfigSet_ReturnsTrueOutValue()
        {
            const string remoteUrl = "http://example.com/foo.git";
            const string section = Constants.GitConfiguration.Credential.SectionName;
            const string property = Constants.GitConfiguration.Credential.Authority;
            var remoteUri = new Uri(remoteUrl);

            const string expectedValue = "provider1";

            var envars = new TestEnvironment();
            var git = new TestGit();
            git.GlobalConfiguration[$"{section}.{property}"] = expectedValue;

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };
            var actualValue = settings.LegacyAuthorityOverride;

            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Settings_LegacyAuthorityOverride_EnvarAndConfigSet_ReturnsEnvarValue()
        {
            const string remoteUrl = "http://example.com/foo.git";
            const string section = Constants.GitConfiguration.Credential.SectionName;
            const string property = Constants.GitConfiguration.Credential.Authority;
            var remoteUri = new Uri(remoteUrl);

            const string expectedValue = "provider1";
            const string otherValue = "provider2";

            var envars = new TestEnvironment
            {
                Variables = {[Constants.EnvironmentVariables.GcmAuthority] = expectedValue}
            };
            var git = new TestGit();
            git.GlobalConfiguration[$"{section}.{property}"] = otherValue;

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };
            var actualValue = settings.LegacyAuthorityOverride;

            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Settings_TryGetSetting_EnvarSet_ReturnsTrueOutValue()
        {
            const string remoteUrl = "http://example.com/foo.git";
            const string envarName = "GCM_TESTVAR";
            const string section = "gcmtest";
            const string property = "bar";
            var remoteUri = new Uri(remoteUrl);

            const string expectedValue = "Hello, World!";

            var envars = new TestEnvironment
            {
                Variables = {[envarName] = expectedValue}
            };
            var git = new TestGit();

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };
            var result = settings.TryGetSetting(envarName, section, property, out string actualValue);

            Assert.True(result);
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Settings_TryGetSetting_EnvarUnset_ReturnsFalse()
        {
            const string remoteUrl = "http://example.com/foo.git";
            const string envarName = "GCM_TESTVAR";
            const string section = "gcmtest";
            const string property = "bar";
            var remoteUri = new Uri(remoteUrl);

            var envars = new TestEnvironment();
            var git = new TestGit();

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };
            var result = settings.TryGetSetting(envarName, section, property, out string actualValue);

            Assert.False(result);
            Assert.Null(actualValue);
        }

        [Fact]
        public void Settings_TryGetSetting_GlobalConfig_ReturnsTrueAndValue()
        {
            const string remoteUrl = "http://example.com/foo.git";
            const string envarName = "GCM_TESTVAR";
            const string section = "gcmtest";
            const string property = "bar";
            var remoteUri = new Uri(remoteUrl);

            const string expectedValue = "Hello, World!";

            var envars = new TestEnvironment();
            var git = new TestGit();
            git.GlobalConfiguration[$"{section}.{property}"] = expectedValue;

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };
            var result = settings.TryGetSetting( envarName, section, property, out string actualValue);

            Assert.True(result);
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Settings_TryGetSetting_RepoConfig_ReturnsTrueAndValue()
        {
            const string remoteUrl = "http://example.com/foo.git";
            const string envarName = "GCM_TESTVAR";
            const string section = "gcmtest";
            const string property = "bar";
            var remoteUri = new Uri(remoteUrl);

            const string expectedValue = "Hello, World!";

            var envars = new TestEnvironment();
            var git = new TestGit();
            git.LocalConfiguration[$"{section}.{property}"] = expectedValue;

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };
            var result = settings.TryGetSetting(envarName, section, property, out string actualValue);

            Assert.True(result);
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Settings_TryGetSetting_ScopedConfig()
        {
            const string remoteUrl = "http://example.com/foo/bar/bazz.git";
            const string scope1 = "example.com";
            const string scope2 = "example.com/foo/bar";
            const string envarName = "GCM_TESTVAR";
            const string section = "gcmtest";
            const string property = "bar";
            var remoteUri = new Uri(remoteUrl);

            const string expectedValue = "Hello, World!";
            const string otherValue = "Goodbye, World!";

            var envars = new TestEnvironment();
            var git = new TestGit();
            git.LocalConfiguration[$"{section}.{scope1}.{property}"] = otherValue;
            git.LocalConfiguration[$"{section}.{scope2}.{property}"] = expectedValue;

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };
            var result = settings.TryGetSetting(envarName, section, property, out string actualValue);

            Assert.True(result);
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Settings_TryGetSetting_EnvarAndConfig_EnvarTakesPrecedence()
        {
            const string remoteUrl = "http://example.com/foo.git";
            const string envarName = "GCM_TESTVAR";
            const string section = "gcmtest";
            const string property = "bar";
            var remoteUri = new Uri(remoteUrl);

            const string expectedValue = "Hello, World!";
            const string otherValue = "Goodbye, World!";

            var envars = new TestEnvironment
            {
                Variables = {[envarName] = expectedValue}
            };
            var git = new TestGit();
            git.LocalConfiguration[$"{section}.{property}"] = otherValue;

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };
            var result = settings.TryGetSetting(envarName, section, property, out string actualValue);

            Assert.True(result);
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Settings_GetSettingValues_EnvarAndMultipleConfig_ReturnsAllWithCorrectPrecedence()
        {
            const string remoteUrl = "http://example.com/foo.git";
            const string scope1 = "http://example.com";
            const string scope2 = "example.com";
            const string envarName = "GCM_TESTVAR";
            const string section = "gcmtest";
            const string property = "bar";
            var remoteUri = new Uri(remoteUrl);

            const string value1 = "First value";
            const string value2 = "Second value";
            const string value3 = "Third value";
            const string value4 = "Last value";

            string[] expectedValues = {value1, value2, value3, value4};

            var envars = new TestEnvironment
            {
                Variables = {[envarName] = value1}
            };
            var git = new TestGit();
            git.LocalConfiguration[$"{section}.{scope1}.{property}"] = value2;
            git.LocalConfiguration[$"{section}.{scope2}.{property}"] = value3;
            git.LocalConfiguration[$"{section}.{property}"]          = value4;

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };
            string[] actualValues = settings.GetSettingValues(envarName, section, property).ToArray();

            Assert.Equal(expectedValues, actualValues);
        }

        [Fact]
        public void Settings_GetSettingValues_ReturnsAllMatchingValues()
        {
            const string remoteUrl = "http://example.com/foo/bar/bazz.git";
            const string broadScope = "example.com";
            const string tightScope = "example.com/foo/bar";
            const string otherScope1 = "test.com";
            const string otherScope2 = "sub.test.com";
            const string envarName = "GCM_TESTVAR";
            const string envarValue = "envar-value";
            const string section = "gcmtest";
            const string property = "bar";
            var remoteUri = new Uri(remoteUrl);

            const string tightScopeValue = "value-scope1";
            const string broadScopeValue = "value-scope2";
            const string noScopeValue = "value-no-scope";
            const string otherValue1 = "other-scope1";
            const string otherValue2 = "other-scope2";

            string[] expectedValues = {envarValue, tightScopeValue, broadScopeValue, noScopeValue};

            var envars = new TestEnvironment
            {
                Variables = {[envarName] = envarValue}
            };

            var git = new TestGit();
            git.LocalConfiguration[$"{section}.{property}"] = noScopeValue;
            git.LocalConfiguration[$"{section}.{broadScope}.{property}"] = broadScopeValue;
            git.LocalConfiguration[$"{section}.{tightScope}.{property}"] = tightScopeValue;
            git.LocalConfiguration[$"{section}.{otherScope1}.{property}"] = otherValue1;
            git.LocalConfiguration[$"{section}.{otherScope2}.{property}"] = otherValue2;

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };

            string[] actualValues = settings.GetSettingValues(envarName, section, property).ToArray();

            Assert.NotNull(actualValues);
            Assert.Equal(expectedValues, actualValues);
        }

        [Fact]
        public void Settings_GetSettingValues_IgnoresSectionAndPropertyCase_ScopeIsCaseSensitive()
        {
            const string remoteUrl = "http://example.com/foo/bar/bazz.git";
            const string scopeLo = "example.com";
            const string scopeHi = "EXAMPLE.COM";
            const string envarName = "GCM_TESTVAR";
            const string envarValue = "envar-value";
            const string sectionLo = "gcmtest";
            const string sectionHi = "GCMTEST";
            const string sectionMix = "GcMtEsT";
            const string propertyLo = "bar";
            const string propertyHi = "BAR";
            const string propertyMix = "bAr";
            var remoteUri = new Uri(remoteUrl);

            const string noScopeValue = "the-value";
            const string lowScopeValue = "value-scope-lo";
            const string highScopeValue = "value-scope-hi";

            string[] expectedValues = {envarValue, lowScopeValue, noScopeValue};

            var envars = new TestEnvironment
            {
                Variables = {[envarName] = envarValue}
            };

            var git = new TestGit();
            git.LocalConfiguration[$"{sectionLo}.{propertyHi}"] = noScopeValue;
            git.LocalConfiguration[$"{sectionHi}.{scopeLo}.{propertyHi}"] = lowScopeValue;
            git.LocalConfiguration[$"{sectionLo}.{scopeHi}.{propertyLo}"] = highScopeValue;

            var settings = new Settings(envars, git)
            {
                RemoteUri = remoteUri
            };

            string[] actualValues = settings.GetSettingValues(envarName, sectionMix, propertyMix).ToArray();

            Assert.NotNull(actualValues);
            Assert.Equal(expectedValues, actualValues);
        }
    }
}
