using System;
using System.Collections.Generic;
using System.Globalization;
using GitCredentialManager;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace Microsoft.AzureRepos.Tests
{
    public class AzureReposAuthorityCacheTests
    {
        [Fact]
        public void AzureReposAuthorityCache_GetAuthority_Null_ThrowException()
        {
            var trace = new NullTrace();
            var git = new TestGit();
            var cache = new AzureDevOpsAuthorityCache(trace, git);

            Assert.Throws<ArgumentNullException>(() => cache.GetAuthority(null));
        }

        [Fact]
        public void AzureReposAuthorityCache_GetAuthority_NoCachedAuthority_ReturnsNull()
        {
            string key = CreateKey("contoso");

            var trace = new NullTrace();
            var git = new TestGit();
            var cache = new AzureDevOpsAuthorityCache(trace, git);

            string authority = cache.GetAuthority(key);

            Assert.Null(authority);
        }

        [Fact]
        public void AzureReposAuthorityCache_GetAuthority_CachedAuthority_ReturnsAuthority()
        {
            const string orgName = "contoso";
            string key = CreateKey(orgName);
            const string expectedAuthority = "https://login.contoso.com";

            var git = new TestGit
            {
                Configuration =
                {
                    Global =
                    {
                        [key] = new[] {expectedAuthority}
                    }
                }
            };

            var trace = new NullTrace();
            var cache = new AzureDevOpsAuthorityCache(trace, git);

            string actualAuthority = cache.GetAuthority(orgName);

            Assert.Equal(expectedAuthority, actualAuthority);
        }

        [Fact]
        public void AzureReposAuthorityCache_UpdateAuthority_NoCachedAuthority_SetsAuthority()
        {
            const string orgName = "contoso";
            string key = CreateKey(orgName);
            const string expectedAuthority = "https://login.contoso.com";

            var trace = new NullTrace();
            var git = new TestGit();
            var cache = new AzureDevOpsAuthorityCache(trace, git);

            cache.UpdateAuthority(orgName, expectedAuthority);

            Assert.True(git.Configuration.Global.TryGetValue(key, out IList<string> values));
            Assert.Single(values);
            string actualAuthority = values[0];
            Assert.Equal(expectedAuthority, actualAuthority);
        }

        [Fact]
        public void AzureReposAuthorityCache_UpdateAuthority_CachedAuthority_UpdatesAuthority()
        {
            const string orgName = "contoso";
            string key = CreateKey(orgName);
            const string oldAuthority = "https://old-login.contoso.com";
            const string expectedAuthority = "https://login.contoso.com";

            var git = new TestGit
            {
                Configuration =
                {
                    Global =
                    {
                        [key] = new[] {oldAuthority}
                    }
                }
            };

            var trace = new NullTrace();
            var cache = new AzureDevOpsAuthorityCache(trace, git);

            cache.UpdateAuthority(orgName, expectedAuthority);

            Assert.True(git.Configuration.Global.TryGetValue(key, out IList<string> values));
            Assert.Single(values);
            string actualAuthority = values[0];
            Assert.Equal(expectedAuthority, actualAuthority);
        }

        [Fact]
        public void AzureReposAuthorityCache_EraseAuthority_NoCachedAuthority_DoesNothing()
        {
            const string orgName = "contoso";
            string key = CreateKey(orgName);
            string otherKey = CreateKey("org.fabrikam.authority");
            const string otherAuthority = "https://fabrikam.com/login";

            var git = new TestGit
            {
                Configuration =
                {
                    Global =
                    {
                        [otherKey] = new[] {otherAuthority}
                    }
                }
            };

            var trace = new NullTrace();
            var cache = new AzureDevOpsAuthorityCache(trace, git);

            cache.EraseAuthority(orgName);

            // Other entries should remain
            Assert.False(git.Configuration.Global.ContainsKey(key));
            Assert.Single(git.Configuration.Global);
            Assert.True(git.Configuration.Global.TryGetValue(otherKey, out IList<string> values));
            Assert.Single(values);
            string actualOtherAuthority = values[0];
            Assert.Equal(otherAuthority, actualOtherAuthority);
        }

        [Fact]
        public void AzureReposAuthorityCache_EraseAuthority_CachedAuthority_RemovesAuthority()
        {
            const string orgName = "contoso";
            string key = CreateKey(orgName);
            const string authority = "https://login.contoso.com";
            string otherKey = CreateKey("fabrikam");
            const string otherAuthority = "https://fabrikam.com/login";

            var git = new TestGit
            {
                Configuration =
                {
                    Global =
                    {
                        [key] = new[] {authority},
                        [otherKey] = new[] {otherAuthority}
                    }
                }
            };

            var trace = new NullTrace();
            var cache = new AzureDevOpsAuthorityCache(trace, git);

            cache.EraseAuthority(orgName);

            // Only the other entries should remain
            Assert.False(git.Configuration.Global.ContainsKey(key));
            Assert.Single(git.Configuration.Global);
            Assert.True(git.Configuration.Global.TryGetValue(otherKey, out IList<string> values));
            Assert.Single(values);
            string actualOtherAuthority = values[0];
            Assert.Equal(otherAuthority, actualOtherAuthority);
        }

        private static string CreateKey(string orgName)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}:{2}/{3}.{4}",
                Constants.GitConfiguration.Credential.SectionName,
                AzureDevOpsConstants.UrnScheme, AzureDevOpsConstants.UrnOrgPrefix, orgName,
                AzureDevOpsConstants.GitConfiguration.Credential.AzureAuthority);
        }
    }
}
