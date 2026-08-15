using System.Collections.Generic;
using System.Threading.Tasks;
using GitCredentialManager;
using GitCredentialManager.Authentication;
using GitCredentialManager.Tests.Objects;
using Moq;
using Xunit;

namespace Microsoft.ManagedApps.Tests
{
    public class ManagedAppsHostProviderTests
    {
        private const string ProdHost = "4899945d6e51f1f0326cda880ec7a7.09.environment.api.powerplatform.com";
        private const string PreprodHost = "c0a12cc2ff79f7306d6ef9fc69c2062.1.environment.api.preprod.powerplatform.com";

        #region IsSupported

        [Fact]
        public void ManagedAppsHostProvider_IsSupported_ProdHost_Https_ReturnsTrue()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = ProdHost,
            });

            var provider = new ManagedAppsHostProvider(new TestCommandContext());

            Assert.True(provider.IsSupported(input));
        }

        [Fact]
        public void ManagedAppsHostProvider_IsSupported_ProdHost_UnencryptedHttp_ReturnsTrue()
        {
            // Reported as supported over HTTP too so that GenerateCredentialAsync can produce
            // a helpful "use HTTPS" error, rather than silently falling through to another provider.
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "http",
                ["host"] = ProdHost,
            });

            var provider = new ManagedAppsHostProvider(new TestCommandContext());

            Assert.True(provider.IsSupported(input));
        }

        [Fact]
        public void ManagedAppsHostProvider_IsSupported_PreprodHost_NotYetComplete_ReturnsFalse()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = PreprodHost,
            });

            var provider = new ManagedAppsHostProvider(new TestCommandContext());

            Assert.False(provider.IsSupported(input));
        }

        [Fact]
        public void ManagedAppsHostProvider_IsSupported_UnrelatedHost_ReturnsFalse()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "example.com",
            });

            var provider = new ManagedAppsHostProvider(new TestCommandContext());

            Assert.False(provider.IsSupported(input));
        }

        [Fact]
        public void ManagedAppsHostProvider_IsSupported_NullInput_ReturnsFalse()
        {
            var provider = new ManagedAppsHostProvider(new TestCommandContext());

            Assert.False(provider.IsSupported((InputArguments)null));
        }

        #endregion

        #region GetServiceName

        [Fact]
        public void ManagedAppsHostProvider_GetServiceName_DropsPathAndUserInfo()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = ProdHost,
                ["path"] = "appframework/git/repositories/9f3a1c4e-8b02-4d17-a5c6-2e7f0b41d38a",
                ["username"] = "someuser",
            });

            var provider = new ManagedAppsHostProvider(new TestCommandContext());

            Assert.Equal($"https://{ProdHost}", provider.GetServiceName(input));
        }

        #endregion

        #region GenerateCredentialAsync - interactive user auth

        [Fact]
        public async Task ManagedAppsHostProvider_GetCredentialAsync_Prod_ReturnsCredentialFromMsal()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = ProdHost,
            });

            var context = new TestCommandContext();
            const string expectedAuthority = "https://login.microsoftonline.com/organizations";
            const string upn = "user@example.com";
            const string accessToken = "ACCESS-TOKEN";

            var msAuthMock = new Mock<IMicrosoftAuthentication>(MockBehavior.Strict);
            msAuthMock
                .Setup(x => x.GetTokenForUserAsync(
                    expectedAuthority,
                    ManagedAppsConstants.AadClientId,
                    ManagedAppsConstants.AadRedirectUri,
                    It.Is<string[]>(s => s.Length == 2 && System.Array.IndexOf(s, "offline_access") >= 0
                        && System.Array.IndexOf(s, "https://api.powerplatform.com/.default") >= 0),
                    null,
                    false))
                .ReturnsAsync(new MockMsAuthResult { AccountUpn = upn, AccessToken = accessToken });

            var bindingMgrMock = new Mock<IManagedAppsBindingManager>(MockBehavior.Strict);
            bindingMgrMock.Setup(x => x.GetAccount($"https://{ProdHost}")).Returns((string)null);

            var provider = new ManagedAppsHostProvider(context, msAuthMock.Object, bindingMgrMock.Object);

            GetCredentialResult result = await provider.GetCredentialAsync(input);

            Assert.Equal(upn, result.Credential.Account);
            Assert.Equal(accessToken, result.Credential.Password);

            // Never consult the OS credential store - there is no PAT-equivalent credential
            // for this provider to cache; MSAL handles its own silent-refresh cache instead.
            Assert.Equal(0, context.CredentialStore.Count);
        }

        [Fact]
        public async Task ManagedAppsHostProvider_GetCredentialAsync_UsesRemoteUserNameOverBindingHint()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = ProdHost,
                ["username"] = "url-user@example.com",
            });

            var context = new TestCommandContext();

            var msAuthMock = new Mock<IMicrosoftAuthentication>(MockBehavior.Strict);
            msAuthMock
                .Setup(x => x.GetTokenForUserAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.Uri>(), It.IsAny<string[]>(),
                    "url-user@example.com", false))
                .ReturnsAsync(new MockMsAuthResult { AccountUpn = "url-user@example.com", AccessToken = "TOKEN" });

            // Binding manager must not even be consulted when the remote URL specifies a user.
            var bindingMgrMock = new Mock<IManagedAppsBindingManager>(MockBehavior.Strict);

            var provider = new ManagedAppsHostProvider(context, msAuthMock.Object, bindingMgrMock.Object);

            await provider.GetCredentialAsync(input);

            msAuthMock.VerifyAll();
        }

        [Fact]
        public async Task ManagedAppsHostProvider_GetCredentialAsync_FallsBackToBindingManagerHint()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = ProdHost,
            });

            var context = new TestCommandContext();

            var msAuthMock = new Mock<IMicrosoftAuthentication>(MockBehavior.Strict);
            msAuthMock
                .Setup(x => x.GetTokenForUserAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.Uri>(), It.IsAny<string[]>(),
                    "bound-user@example.com", false))
                .ReturnsAsync(new MockMsAuthResult { AccountUpn = "bound-user@example.com", AccessToken = "TOKEN" });

            var bindingMgrMock = new Mock<IManagedAppsBindingManager>(MockBehavior.Strict);
            bindingMgrMock.Setup(x => x.GetAccount($"https://{ProdHost}")).Returns("bound-user@example.com");

            var provider = new ManagedAppsHostProvider(context, msAuthMock.Object, bindingMgrMock.Object);

            await provider.GetCredentialAsync(input);

            msAuthMock.VerifyAll();
        }

        [Fact]
        public async Task ManagedAppsHostProvider_GenerateCredentialAsync_UnrecognizedHost_Throws()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "example.com",
            });

            var provider = new ManagedAppsHostProvider(new TestCommandContext(),
                Mock.Of<IMicrosoftAuthentication>(), Mock.Of<IManagedAppsBindingManager>());

            await Assert.ThrowsAnyAsync<System.Exception>(() => provider.GenerateCredentialAsync(input));
        }

        [Fact]
        public async Task ManagedAppsHostProvider_GenerateCredentialAsync_UnencryptedHttp_ThrowsByDefault()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "http",
                ["host"] = ProdHost,
            });

            var provider = new ManagedAppsHostProvider(new TestCommandContext(),
                Mock.Of<IMicrosoftAuthentication>(), Mock.Of<IManagedAppsBindingManager>());

            await Assert.ThrowsAnyAsync<System.Exception>(() => provider.GenerateCredentialAsync(input));
        }

        [Fact]
        public async Task ManagedAppsHostProvider_GenerateCredentialAsync_UnencryptedHttp_AllowUnsafeRemotes_Succeeds()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "http",
                ["host"] = ProdHost,
            });

            var context = new TestCommandContext();
            context.Settings.AllowUnsafeRemotes = true;

            var msAuthMock = new Mock<IMicrosoftAuthentication>();
            msAuthMock
                .Setup(x => x.GetTokenForUserAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.Uri>(), It.IsAny<string[]>(),
                    It.IsAny<string>(), false))
                .ReturnsAsync(new MockMsAuthResult { AccountUpn = "user@example.com", AccessToken = "TOKEN" });

            var bindingMgrMock = new Mock<IManagedAppsBindingManager>();
            bindingMgrMock.Setup(x => x.GetAccount(It.IsAny<string>())).Returns((string)null);

            var provider = new ManagedAppsHostProvider(context, msAuthMock.Object, bindingMgrMock.Object);

            ICredential credential = await provider.GenerateCredentialAsync(input);

            Assert.Equal("user@example.com", credential.Account);
        }

        #endregion

        #region GenerateCredentialAsync - non-interactive modes

        [Fact]
        public async Task ManagedAppsHostProvider_GenerateCredentialAsync_ManagedIdentity_UsesResourceNotScopes()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = ProdHost,
            });

            var context = new TestCommandContext();
            context.Environment.Variables[ManagedAppsConstants.EnvironmentVariables.ManagedIdentity] = "system";

            var msAuthMock = new Mock<IMicrosoftAuthentication>(MockBehavior.Strict);
            msAuthMock
                .Setup(x => x.GetTokenForManagedIdentityAsync("system", "https://api.powerplatform.com"))
                .ReturnsAsync(new MockMsAuthResult { AccessToken = "MI-TOKEN" });

            var provider = new ManagedAppsHostProvider(context, msAuthMock.Object, Mock.Of<IManagedAppsBindingManager>());

            ICredential credential = await provider.GenerateCredentialAsync(input);

            Assert.Equal("system", credential.Account);
            Assert.Equal("MI-TOKEN", credential.Password);
        }

        [Fact]
        public async Task ManagedAppsHostProvider_GenerateCredentialAsync_ServicePrincipal_UsesScopes()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = ProdHost,
            });

            var context = new TestCommandContext();
            context.Environment.Variables[ManagedAppsConstants.EnvironmentVariables.ServicePrincipalId] =
                "11111111-1111-1111-1111-111111111111/22222222-2222-2222-2222-222222222222";
            context.Environment.Variables[ManagedAppsConstants.EnvironmentVariables.ServicePrincipalSecret] = "shh";

            var msAuthMock = new Mock<IMicrosoftAuthentication>(MockBehavior.Strict);
            msAuthMock
                .Setup(x => x.GetTokenForServicePrincipalAsync(
                    It.Is<ServicePrincipalIdentity>(sp =>
                        sp.TenantId == "11111111-1111-1111-1111-111111111111" &&
                        sp.Id == "22222222-2222-2222-2222-222222222222" &&
                        sp.ClientSecret == "shh"),
                    It.Is<string[]>(s => s.Length == 2)))
                .ReturnsAsync(new MockMsAuthResult { AccessToken = "SP-TOKEN" });

            var provider = new ManagedAppsHostProvider(context, msAuthMock.Object, Mock.Of<IManagedAppsBindingManager>());

            ICredential credential = await provider.GenerateCredentialAsync(input);

            Assert.Equal("22222222-2222-2222-2222-222222222222", credential.Account);
            Assert.Equal("SP-TOKEN", credential.Password);
        }

        [Fact]
        public async Task ManagedAppsHostProvider_GenerateCredentialAsync_WorkloadFederationGeneric_UsesScopes()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = ProdHost,
            });

            var context = new TestCommandContext();
            context.Environment.Variables[ManagedAppsConstants.EnvironmentVariables.WorkloadFederation] = "generic";
            context.Environment.Variables[ManagedAppsConstants.EnvironmentVariables.WorkloadFederationClientId] =
                "11111111-1111-1111-1111-111111111111";
            context.Environment.Variables[ManagedAppsConstants.EnvironmentVariables.WorkloadFederationTenantId] =
                "22222222-2222-2222-2222-222222222222";
            context.Environment.Variables[ManagedAppsConstants.EnvironmentVariables.WorkloadFederationAssertion] =
                "eyJhbGci...";

            var msAuthMock = new Mock<IMicrosoftAuthentication>(MockBehavior.Strict);
            msAuthMock
                .Setup(x => x.GetTokenUsingWorkloadFederationAsync(
                    It.Is<MicrosoftWorkloadFederationOptions>(o =>
                        o.Scenario == MicrosoftWorkloadFederationScenario.Generic &&
                        o.GenericClientAssertion == "eyJhbGci..."),
                    It.Is<string[]>(s => s.Length == 2)))
                .ReturnsAsync(new MockMsAuthResult { AccessToken = "WIF-TOKEN" });

            var provider = new ManagedAppsHostProvider(context, msAuthMock.Object, Mock.Of<IManagedAppsBindingManager>());

            ICredential credential = await provider.GenerateCredentialAsync(input);

            Assert.Equal("11111111-1111-1111-1111-111111111111", credential.Account);
            Assert.Equal("WIF-TOKEN", credential.Password);
        }

        #endregion

        #region Store / Erase

        [Fact]
        public async Task ManagedAppsHostProvider_StoreCredentialAsync_Interactive_RecordsBinding()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = ProdHost,
                ["username"] = "user@example.com",
            });

            var bindingMgrMock = new Mock<IManagedAppsBindingManager>(MockBehavior.Strict);
            bindingMgrMock.Setup(x => x.SignIn($"https://{ProdHost}", "user@example.com"));

            var provider = new ManagedAppsHostProvider(new TestCommandContext(),
                Mock.Of<IMicrosoftAuthentication>(), bindingMgrMock.Object);

            await provider.StoreCredentialAsync(input);

            bindingMgrMock.VerifyAll();
        }

        [Fact]
        public async Task ManagedAppsHostProvider_StoreCredentialAsync_ManagedIdentity_DoesNotRecordBinding()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = ProdHost,
            });

            var context = new TestCommandContext();
            context.Environment.Variables[ManagedAppsConstants.EnvironmentVariables.ManagedIdentity] = "system";

            // Strict mock with no setups - any call would throw.
            var bindingMgrMock = new Mock<IManagedAppsBindingManager>(MockBehavior.Strict);

            var provider = new ManagedAppsHostProvider(context, Mock.Of<IMicrosoftAuthentication>(), bindingMgrMock.Object);

            await provider.StoreCredentialAsync(input);
        }

        [Fact]
        public async Task ManagedAppsHostProvider_EraseCredentialAsync_Interactive_RemovesBinding()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = ProdHost,
            });

            var bindingMgrMock = new Mock<IManagedAppsBindingManager>(MockBehavior.Strict);
            bindingMgrMock.Setup(x => x.SignOut($"https://{ProdHost}"));

            var provider = new ManagedAppsHostProvider(new TestCommandContext(),
                Mock.Of<IMicrosoftAuthentication>(), bindingMgrMock.Object);

            await provider.EraseCredentialAsync(input);

            bindingMgrMock.VerifyAll();
        }

        #endregion

        private class MockMsAuthResult : IMicrosoftAuthenticationResult
        {
            public string AccessToken { get; set; }
            public string AccountUpn { get; set; }
        }
    }
}
