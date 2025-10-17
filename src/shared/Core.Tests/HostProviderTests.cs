using System;
using System.Collections.Generic;
using System.Runtime;
using System.Threading.Tasks;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace GitCredentialManager.Tests
{
    public class HostProviderTests
    {
        #region GetCredentialAsync

        [Fact]
        public async Task HostProvider_GetCredentialAsync_CredentialExists_ReturnsExistingCredential()
        {
            const string userName = "john.doe";
            const string password = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]
            const string service = "https://example.com";
            const string refreshToken = "xyzzy";
            DateTimeOffset expiry = DateTimeOffset.FromUnixTimeSeconds(1919539847);
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "example.com",
            });

            var context = new TestCommandContext();
            context.CredentialStore.Add(service, new TestCredential(service, userName, password) { OAuthRefreshToken = refreshToken, PasswordExpiry = expiry});
            var provider = new TestHostProvider(context)
            {
                IsSupportedFunc = _ => true,
                GenerateCredentialFunc = _ =>
                {
                    Assert.Fail("Should never be called");
                    return null;
                },
            };

            ICredential actualCredential = await ((IHostProvider) provider).GetCredentialAsync(input);

            Assert.Equal(userName, actualCredential.Account);
            Assert.Equal(password, actualCredential.Password);
            Assert.Equal(refreshToken, actualCredential.OAuthRefreshToken);
            Assert.Equal(expiry, actualCredential.PasswordExpiry);
        }

        [Fact]
        public async Task HostProvider_GetCredentialAsync_CredentialDoesNotExist_ReturnsNewGeneratedCredential()
        {
            const string userName = "john.doe";
            const string password = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "example.com",
            });

            bool generateWasCalled = false;
            var context = new TestCommandContext();
            var provider = new TestHostProvider(context)
            {
                IsSupportedFunc = _ => true,
                GenerateCredentialFunc = _ =>
                {
                    generateWasCalled = true;
                    return new GitCredential(userName, password);
                },
            };

            ICredential actualCredential = await ((IHostProvider) provider).GetCredentialAsync(input);

            Assert.True(generateWasCalled);
            Assert.Equal(userName, actualCredential.Account);
            Assert.Equal(password, actualCredential.Password);
        }

        [Fact]
        public async Task HostProvider_GetCredentialAsync_InvalidCredentialsStored_ReturnsNewGeneratedCredential()
        {
            const string userName = "john.doe";
            const string password = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]
            const string service = "https://example.com";
            const string storedRefreshToken = "first";
            const string refreshToken = "second";
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "example.com",
            });

            bool generateWasCalled = false;
            string refreshTokenSeenByGenerate = null;
            var context = new TestCommandContext();
            context.CredentialStore.Add(service, new TestCredential(service, "stored-user", "stored-password") { OAuthRefreshToken = storedRefreshToken});
            context.CredentialStore.Add(service, new TestCredential(service, "another-stored-user", "another-stored-password"));
            var provider = new TestHostProvider(context)
            {
                ValidateCredentialFunc = (_, _) => false,
                IsSupportedFunc = _ => true,
                GenerateCredentialFunc = input =>
                {
                    generateWasCalled = true;
                    refreshTokenSeenByGenerate = input.OAuthRefreshToken;
                    return new GitCredential(userName, password) {
                        OAuthRefreshToken = refreshToken,
                    };
                },
            };

            ICredential actualCredential = await ((IHostProvider) provider).GetCredentialAsync(input);

            Assert.True(generateWasCalled);
            Assert.Equal(storedRefreshToken, refreshTokenSeenByGenerate);
            Assert.Equal(userName, actualCredential.Account);
            Assert.Equal(password, actualCredential.Password);
            Assert.Equal(refreshToken, actualCredential.OAuthRefreshToken);
            // Invalid credentials should be erased
            Assert.Equal(0, context.CredentialStore.Count);
        }


            #endregion

        #region StoreCredentialAsync

        [Fact]
        public async Task HostProvider_StoreCredentialAsync_EmptyCredential_DoesNotStoreCredential()
        {
            const string emptyUserName = "";
            const string emptyPassword = ""; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "example.com",
                ["username"] = emptyUserName,
                ["password"] = emptyPassword, // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]
            });

            var context = new TestCommandContext();
            var provider = new TestHostProvider(context);

            await ((IHostProvider) provider).StoreCredentialAsync(input);

            Assert.Equal(0, context.CredentialStore.Count);
        }

        [Fact]
        public async Task HostProvider_StoreCredentialAsync_NonEmptyCredential_StoresCredential()
        {
            const string userName = "john.doe";
            const string password = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]
            const string service = "https://example.com";
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "example.com",
                ["username"] = userName,
                ["password"] = password, // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]
            });

            var context = new TestCommandContext();
            var provider = new TestHostProvider(context);

            await ((IHostProvider) provider).StoreCredentialAsync(input);

            Assert.Equal(1, context.CredentialStore.Count);
            Assert.True(context.CredentialStore.TryGet(service, userName, out var storedCredential));
            Assert.Equal(userName, storedCredential.Account);
            Assert.Equal(password, storedCredential.Password);
        }

        [Fact]
        public async Task HostProvider_StoreCredentialAsync_NonEmptyCredential_ExistingCredential_UpdatesCredential()
        {
            const string testUserName = "john.doe";
            const string testPasswordOld = "letmein123-old"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]
            const string testPasswordNew = "letmein123-new"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]
            const string testService = "https://example.com";
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "example.com",
                ["username"] = testUserName,
                ["password"] = testPasswordNew, // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]
            });

            var context = new TestCommandContext();
            context.CredentialStore.Add(testService, testUserName, testPasswordOld);
            var provider = new TestHostProvider(context);

            await ((IHostProvider) provider).StoreCredentialAsync(input);

            Assert.Equal(1, context.CredentialStore.Count);
            Assert.True(context.CredentialStore.TryGet(testService, testUserName, out var storedCredential));
            Assert.Equal(testUserName, storedCredential.Account);
            Assert.Equal(testPasswordNew, storedCredential.Password);
        }

        #endregion

        #region EraseCredentialAsync

        [Fact]
        public async Task HostProvider_EraseCredentialAsync_NoInputUser_CredentialExists_ErasesOneCredential()
        {
            const string service = "https://example.com";
            const string userName1 = "john.doe";
            const string userName2 = "alice";
            const string userName3 = "bob";
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "example.com",
            });

            var context = new TestCommandContext();
            context.CredentialStore.Add(service, userName1, "letmein123");
            context.CredentialStore.Add(service, userName2, "do-not-erase-me");
            context.CredentialStore.Add(service, userName3, "here-forever");
            var provider = new TestHostProvider(context);

            await ((IHostProvider) provider).EraseCredentialAsync(input);

            Assert.Equal(2, context.CredentialStore.Count);
        }

        [Fact]
        public async Task HostProvider_EraseCredentialAsync_InputUser_CredentialExists_UserNotMatch_DoesNothing()
        {
            const string userName1 = "john.doe";
            const string userName2 = "alice";
            const string password = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]
            const string service = "https://example.com";
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "example.com",
                ["username"] = userName1,
                ["password"] = password, // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]
            });

            var context = new TestCommandContext();
            context.CredentialStore.Add(service, userName2, password);
            var provider = new TestHostProvider(context);

            await ((IHostProvider) provider).EraseCredentialAsync(input);

            Assert.Equal(1, context.CredentialStore.Count);
            Assert.True(context.CredentialStore.Contains(service, userName2));
        }

        [Fact]
        public async Task HostProvider_EraseCredentialAsync_InputUser_CredentialExists_UserMatch_ErasesCredential()
        {
            const string userName = "john.doe";
            const string password = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]
            const string service = "https://example.com";
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "example.com",
                ["username"] = userName,
                ["password"] = password, // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]
            });

            var context = new TestCommandContext();
            context.CredentialStore.Add(service, userName, password);
            var provider = new TestHostProvider(context);

            await ((IHostProvider) provider).EraseCredentialAsync(input);

            Assert.Equal(0, context.CredentialStore.Count);
            Assert.False(context.CredentialStore.Contains(service, userName));
        }

        [Fact]
        public async Task HostProvider_EraseCredentialAsync_DifferentHost_DoesNothing()
        {
            const string service2 = "https://example2.com";
            const string service3 = "https://example3.com";
            const string userName = "john.doe";
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "example1.com",
            });

            var context = new TestCommandContext();
            context.CredentialStore.Add(service2, userName, "keep-me");
            context.CredentialStore.Add(service3, userName, "also-keep-me");
            var provider = new TestHostProvider(context);

            await ((IHostProvider) provider).EraseCredentialAsync(input);

            Assert.Equal(2, context.CredentialStore.Count);
            Assert.True(context.CredentialStore.Contains(service2, userName));
            Assert.True(context.CredentialStore.Contains(service3, userName));
        }

        [Fact]
        public async Task HostProvider_ValidateCredentialAsync()
        {
            var context = new TestCommandContext();
            var provider = new TestHostProvider(context);
            Assert.True(await provider.ValidateCredentialAsync(null, new GitCredential("username", "pass")));
            Assert.True(await provider.ValidateCredentialAsync(null, new GitCredential("username", "pass") {PasswordExpiry
             = DateTimeOffset.UtcNow + TimeSpan.FromHours(1)}));
            Assert.False(await provider.ValidateCredentialAsync(null, new GitCredential("username", "pass") {PasswordExpiry
             = DateTimeOffset.UtcNow - TimeSpan.FromMinutes(1)}));
        }

        #endregion
    }
}
