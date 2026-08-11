using System.Collections.Generic;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace Microsoft.ManagedApps.Tests
{
    public class ManagedAppsCloudEnvironmentTests
    {
        private static readonly ManagedAppsCloudEnvironment CompleteCloudEnvironment = new ManagedAppsCloudEnvironment(
            "prod",
            ".environment.api.powerplatform.com",
            "https://api.powerplatform.com",
            new[] { "https://api.powerplatform.com/GitRepositories.Repositories.Read", "offline_access" });

        private static readonly ManagedAppsCloudEnvironment IncompleteCloudEnvironment = new ManagedAppsCloudEnvironment(
            "preprod",
            ".environment.api.preprod.powerplatform.com",
            null,
            null);

        #region IsComplete

        [Fact]
        public void ManagedAppsCloudEnvironment_IsComplete_AllFieldsPresent_ReturnsTrue()
        {
            Assert.True(CompleteCloudEnvironment.IsComplete);
        }

        [Fact]
        public void ManagedAppsCloudEnvironment_IsComplete_MissingResourceAndScopes_ReturnsFalse()
        {
            Assert.False(IncompleteCloudEnvironment.IsComplete);
        }

        [Fact]
        public void ManagedAppsCloudEnvironment_IsComplete_EmptyScopesList_ReturnsFalse()
        {
            var cloudEnvironment = new ManagedAppsCloudEnvironment("x", "suffix", "https://resource", new string[0]);
            Assert.False(cloudEnvironment.IsComplete);
        }

        #endregion

        #region TryMatch (pure matching logic)

        [Theory]
        [InlineData("335c2de2afba6fd0e64fbed1b077de.09.environment.api.powerplatform.com")]
        [InlineData("4899945d6e51f1f0326cda880ec7a7.09.environment.api.powerplatform.com")]
        public void ManagedAppsCloudEnvironment_TryMatch_CompleteCloudEnvironment_MatchesExampleHosts(string host)
        {
            var candidates = new[] { CompleteCloudEnvironment, IncompleteCloudEnvironment };

            bool result = ManagedAppsCloudEnvironment.TryMatch(new NullTrace(), candidates, host, out ManagedAppsCloudEnvironment cloudEnvironment);

            Assert.True(result);
            Assert.Equal("prod", cloudEnvironment.Name);
        }

        [Theory]
        [InlineData("2060e1f7f2d321d53089d1d9a07569e.6.environment.api.preprod.powerplatform.com")]
        [InlineData("c0a12cc2ff79f7306d6ef9fc69c2062.1.environment.api.preprod.powerplatform.com")]
        public void ManagedAppsCloudEnvironment_TryMatch_IncompleteCloudEnvironment_IsNeverMatched(string host)
        {
            // Regression guard for the design rule: a cloud environment only participates in
            // host matching once it is fully specified (host suffix + resource + scopes).
            var candidates = new[] { CompleteCloudEnvironment, IncompleteCloudEnvironment };

            bool result = ManagedAppsCloudEnvironment.TryMatch(new NullTrace(), candidates, host, out ManagedAppsCloudEnvironment cloudEnvironment);

            Assert.False(result);
            Assert.Null(cloudEnvironment);
        }

        [Theory]
        [InlineData("powerplatform.com")]
        [InlineData("api.powerplatform.com")]
        [InlineData("xenvironment.api.powerplatform.com")]
        [InlineData("environment.api.powerplatform.com.attacker.example")]
        [InlineData("evil.example/environment.api.powerplatform.com")]
        [InlineData("")]
        [InlineData(null)]
        public void ManagedAppsCloudEnvironment_TryMatch_LookalikeOrInvalidHosts_ReturnsFalse(string host)
        {
            var candidates = new[] { CompleteCloudEnvironment, IncompleteCloudEnvironment };

            bool result = ManagedAppsCloudEnvironment.TryMatch(new NullTrace(), candidates, host, out ManagedAppsCloudEnvironment cloudEnvironment);

            Assert.False(result);
            Assert.Null(cloudEnvironment);
        }

        [Fact]
        public void ManagedAppsCloudEnvironment_TryMatch_LongestSuffixWins()
        {
            var shortSuffixCloudEnvironment = new ManagedAppsCloudEnvironment(
                "short", ".powerplatform.com", "https://short", new[] { "s" });
            var longSuffixCloudEnvironment = new ManagedAppsCloudEnvironment(
                "long", ".environment.api.powerplatform.com", "https://long", new[] { "l" });

            bool result = ManagedAppsCloudEnvironment.TryMatch(
                new NullTrace(),
                new[] { shortSuffixCloudEnvironment, longSuffixCloudEnvironment },
                "335c2de2afba6fd0e64fbed1b077de.09.environment.api.powerplatform.com",
                out ManagedAppsCloudEnvironment cloudEnvironment);

            Assert.True(result);
            Assert.Equal("long", cloudEnvironment.Name);
        }

        #endregion

        #region Compiled-in defaults

        [Fact]
        public void ManagedAppsCloudEnvironment_CompiledInDefaults_ContainsExpectedNames()
        {
            var names = new List<string>();
            foreach (ManagedAppsCloudEnvironment cloudEnvironment in ManagedAppsCloudEnvironment.CompiledInDefaults)
            {
                names.Add(cloudEnvironment.Name);
            }

            Assert.Contains("prod", names);
            Assert.Contains("preprod", names);
            Assert.Contains("test", names);
        }

        [Fact]
        public void ManagedAppsCloudEnvironment_CompiledInDefaults_OnlyProdIsCompleteToday()
        {
            foreach (ManagedAppsCloudEnvironment cloudEnvironment in ManagedAppsCloudEnvironment.CompiledInDefaults)
            {
                if (cloudEnvironment.Name == "prod")
                {
                    Assert.True(cloudEnvironment.IsComplete);
                }
                else
                {
                    // preprod/test: host suffix known, resource/scopes not yet defined by
                    // the service - must remain incomplete until deliberately completed.
                    Assert.False(cloudEnvironment.IsComplete);
                }
            }
        }

        [Theory]
        [InlineData("335c2de2afba6fd0e64fbed1b077de.09.environment.api.powerplatform.com")]
        [InlineData("4899945d6e51f1f0326cda880ec7a7.09.environment.api.powerplatform.com")]
        public void ManagedAppsCloudEnvironment_CompiledInDefaults_MatchesProdExampleHosts(string host)
        {
            bool result = ManagedAppsCloudEnvironment.TryMatch(new NullTrace(), ManagedAppsCloudEnvironment.CompiledInDefaults, host, out ManagedAppsCloudEnvironment cloudEnvironment);

            Assert.True(result);
            Assert.Equal("prod", cloudEnvironment.Name);
            Assert.Equal(2, cloudEnvironment.Scopes.Count);
            Assert.Contains("offline_access", cloudEnvironment.Scopes);
            Assert.Contains("https://api.powerplatform.com/.default", cloudEnvironment.Scopes);
        }

        [Theory]
        [InlineData("2060e1f7f2d321d53089d1d9a07569e.6.environment.api.preprod.powerplatform.com")]
        [InlineData("c0a12cc2ff79f7306d6ef9fc69c2062.1.environment.api.preprod.powerplatform.com")]
        [InlineData("a0eb0a858adcee649ed990b1c9f25aa.8.environment.api.test.powerplatform.com")]
        [InlineData("276050cb757bff044120192320a7614.4.environment.api.test.powerplatform.com")]
        public void ManagedAppsCloudEnvironment_CompiledInDefaults_DoesNotYetMatchPreprodOrTestExampleHosts(string host)
        {
            // Regression guard: these hosts must keep falling through to the next
            // provider (e.g. the generic OAuth provider) until preprod/test are completed.
            bool result = ManagedAppsCloudEnvironment.TryMatch(new NullTrace(), ManagedAppsCloudEnvironment.CompiledInDefaults, host, out ManagedAppsCloudEnvironment cloudEnvironment);

            Assert.False(result);
            Assert.Null(cloudEnvironment);
        }

        #endregion

        #region GetEffectiveCloudEnvironments (configuration merge)

        [Fact]
        public void ManagedAppsCloudEnvironment_GetEffectiveCloudEnvironments_AddsBrandNewCustomCloudEnvironment()
        {
            var git = new TestGit();
            git.Configuration.Global["credential.managedAppsCloudEnvironment.gov.hostSuffix"] =
                new List<string> { ".environment.api.gov.powerplatform.com" };
            git.Configuration.Global["credential.managedAppsCloudEnvironment.gov.resource"] =
                new List<string> { "https://api.gov.powerplatform.com" };
            git.Configuration.Global["credential.managedAppsCloudEnvironment.gov.scopes"] =
                new List<string> { "https://api.gov.powerplatform.com/GitRepositories.Repositories.Read offline_access" };

            IReadOnlyList<ManagedAppsCloudEnvironment> cloudEnvironments = ManagedAppsCloudEnvironment.GetEffectiveCloudEnvironments(git.Configuration);

            ManagedAppsCloudEnvironment govCloudEnvironment = FindCloudEnvironment(cloudEnvironments, "gov");
            Assert.NotNull(govCloudEnvironment);
            Assert.True(govCloudEnvironment.IsComplete);
            Assert.Equal(".environment.api.gov.powerplatform.com", govCloudEnvironment.HostSuffix);
            Assert.Equal("https://api.gov.powerplatform.com", govCloudEnvironment.ResourceAudience);
            Assert.Contains("offline_access", govCloudEnvironment.Scopes);

            bool matched = ManagedAppsCloudEnvironment.TryMatch(
                new NullTrace(), cloudEnvironments, "abc123.09.environment.api.gov.powerplatform.com", out ManagedAppsCloudEnvironment matchedCloudEnvironment);
            Assert.True(matched);
            Assert.Equal("gov", matchedCloudEnvironment.Name);
        }

        [Fact]
        public void ManagedAppsCloudEnvironment_GetEffectiveCloudEnvironments_CompletesPartialCompiledInCloudEnvironment_FieldLevelMerge()
        {
            var git = new TestGit();
            // Supply only the missing fields for the compiled-in "preprod" cloud environment -
            // the host suffix should still come from the compiled-in default (field-level merge).
            git.Configuration.Global["credential.managedAppsCloudEnvironment.preprod.resource"] =
                new List<string> { "https://api.preprod.powerplatform.com" };
            git.Configuration.Global["credential.managedAppsCloudEnvironment.preprod.scopes"] =
                new List<string> { "https://api.preprod.powerplatform.com/GitRepositories.Repositories.Read offline_access" };

            IReadOnlyList<ManagedAppsCloudEnvironment> cloudEnvironments = ManagedAppsCloudEnvironment.GetEffectiveCloudEnvironments(git.Configuration);

            ManagedAppsCloudEnvironment preprodCloudEnvironment = FindCloudEnvironment(cloudEnvironments, "preprod");
            Assert.NotNull(preprodCloudEnvironment);
            Assert.True(preprodCloudEnvironment.IsComplete);
            Assert.Equal(".environment.api.preprod.powerplatform.com", preprodCloudEnvironment.HostSuffix); // from compiled-in default
            Assert.Equal("https://api.preprod.powerplatform.com", preprodCloudEnvironment.ResourceAudience); // from configuration
        }

        [Fact]
        public void ManagedAppsCloudEnvironment_GetEffectiveCloudEnvironments_NoConfiguration_ReturnsCompiledInDefaultsOnly()
        {
            var git = new TestGit();

            IReadOnlyList<ManagedAppsCloudEnvironment> cloudEnvironments = ManagedAppsCloudEnvironment.GetEffectiveCloudEnvironments(git.Configuration);

            Assert.Equal(ManagedAppsCloudEnvironment.CompiledInDefaults.Count, cloudEnvironments.Count);
        }

        private static ManagedAppsCloudEnvironment FindCloudEnvironment(IEnumerable<ManagedAppsCloudEnvironment> cloudEnvironments, string name)
        {
            foreach (ManagedAppsCloudEnvironment cloudEnvironment in cloudEnvironments)
            {
                if (cloudEnvironment.Name == name)
                {
                    return cloudEnvironment;
                }
            }

            return null;
        }

        #endregion
    }
}
