using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.Authentication.OAuth;
using Microsoft.Git.CredentialManager.Tests.Objects;
using Moq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using Xunit;

namespace Atlassian.Bitbucket.Tests
{
    public class BitbucketHostProviderTest
    {
        private const string MOCK_ACCESS_TOKEN = "at-0987654321";
        private const string MOCK_REFRESH_TOKEN = "rt-1234567809";
        private Mock<IBitbucketAuthentication> bitbucketAuthentication = new Mock<IBitbucketAuthentication>(MockBehavior.Strict);
        private Mock<IBitbucketRestApi> bitbucketApi = new Mock<IBitbucketRestApi>(MockBehavior.Strict);

        [Theory]
        // We report that we support unencrypted HTTP here so that we can fail and
        // show a helpful error message in the call to `GenerateCredentialAsync` instead.
        [InlineData("http", "bitbucket.org", true)]
        [InlineData("ssh", "bitbucket.org", false)]
        [InlineData("https", "bitbucket.org", true)]
        [InlineData("https", "api.bitbucket.org", true)] // Currently does support sub domains.

        [InlineData("https", "bitbucket.ogg", false)] // No support of phony similar tld.
        [InlineData("https", "bitbucket.com", false)] // No support of wrong tld.
        [InlineData("https", "example.com", false)] // No support of non bitbucket domains.

        [InlineData("http", "bitbucket.my-company-server.com", false)]  // Currently no support for named on-premise instances
        [InlineData("https", "my-company-server.com", false)]
        [InlineData("https", "bitbucket.my.company.server.com", false)]
        [InlineData("https", "api.bitbucket.my-company-server.com", false)]
        [InlineData("https", "BITBUCKET.My-Company-Server.Com", false)]
        public void BitbucketHostProvider_IsSupported(string protocol, string host, bool expected)
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = protocol,
                ["host"] = host,
            });

            var provider = new BitbucketHostProvider(new TestCommandContext());
            Assert.Equal(expected, provider.IsSupported(input));
        }

        [Theory]
        [InlineData("X-AREQUESTID", "123456789", true)] // only the specific header is acceptable
        [InlineData("X-REQUESTID", "123456789", false)]
        [InlineData(null, null, false)]
        public void BitbucketHostProvider_IsSupported_HttpResponseMessage(string header, string value, bool expected)
        {
            var input = new HttpResponseMessage();
            if (header != null)
            {
                input.Headers.Add(header, value);
            }

            var provider = new BitbucketHostProvider(new TestCommandContext());
            Assert.Equal(expected, provider.IsSupported(input));
        }


        [Theory]
        [InlineData("https", "bitbucket.org", "jsquire", "password", false, false, false)]
        [InlineData("https", "bitbucket.org", "jsquire", "password", false, true, true)]
        [InlineData("https", "bitbucket.org", "jsquire", "password", true, null, true)]
        // Basic Auth works
        public void BitbucketHostProvider_GetCredentialAsync_ForBasicAuth(string protocol, string host, string username,string password, bool storedAccount, bool? userEntersCredentials, bool expected)
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = protocol,
                ["host"] = host,
                ["username"] = username
            });

            var context = new TestCommandContext();

            if (storedAccount)
            {
                MockStoredBasicAccount(context, input, password);
            }

            if (userEntersCredentials.HasValue && userEntersCredentials.Value)
            {
                MockUserEnteredBasicCredentials(bitbucketAuthentication, input, password);
            }
            else
            {
                MockUserDidNotEnteredBasicCredentials(bitbucketAuthentication);
            }

            MockValidRemoteAccount(bitbucketApi, input, password, false);

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, bitbucketApi.Object);

            if (userEntersCredentials.HasValue && !userEntersCredentials.Value)
            {
                Assert.ThrowsAsync<Exception>(async () => await provider.GetCredentialAsync(input));
                return;
            }

            var credential = provider.GetCredentialAsync(input);

            VerifyBasicAuthFlowRan(password, storedAccount, expected, input, credential);

            VerifyOAuthFlowWasNotRun(password, storedAccount, expected, input, credential);
        }

        [Theory]
        [InlineData("https", "bitbucket.org", "jsquire", "password", true, null, true)]
        [InlineData("https", "bitbucket.org", "jsquire", "password", false, true, true)]
        [InlineData("https", "bitbucket.org", "jsquire", "password", false, null, false)]
        // Basic Auth works
        public void BitbucketHostProvider_GetCredentialAsync_ForOAuth(string protocol, string host, string username, string password, bool storedAccount, bool? userEntersCredentials, bool expected)
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = protocol,
                ["host"] = host,
                ["username"] = username
            });

            var context = new TestCommandContext();

            if (storedAccount)
            {
                MockStoredOAuthAccount(context, input);
            }

            if (userEntersCredentials.HasValue && userEntersCredentials.Value)
            {
                MockUserEnteredBasicCredentials(bitbucketAuthentication, input, password);
            }
            else
            {
                MockUserDidNotEnteredBasicCredentials(bitbucketAuthentication);
            }

            MockValidRemoteAccount(bitbucketApi, input, password, true);
            MockRemoteValidRefreshToken();

            var provider = new BitbucketHostProvider(context, bitbucketAuthentication.Object, bitbucketApi.Object);

            if (userEntersCredentials.HasValue && !userEntersCredentials.Value)
            {
                Assert.ThrowsAsync<Exception>(async () => await provider.GetCredentialAsync(input));
                return;
            }

            var credential = provider.GetCredentialAsync(input);

            VerifyOAuthFlowRan(password, storedAccount, expected, input, credential);

            VerifyBasicAuthFlowDidNotRun(password, storedAccount, expected, input, credential);
        }

        private void VerifyBasicAuthFlowRan(string password, bool storedAccount, bool expected, InputArguments input, System.Threading.Tasks.Task<ICredential> credential)
        {
            Assert.Equal(expected, credential != null);

            var remoteUri = input.GetRemoteUri();

            if (storedAccount)
            {
                // stored username/password so no need to prompt the user for them
                bitbucketAuthentication.Verify(m => m.GetBasicCredentialsAsync(remoteUri, input.UserName), Times.Never);
            }
            else
            {
                // no username/password credentials so prompt the user for them
                bitbucketAuthentication.Verify(m => m.GetBasicCredentialsAsync(remoteUri, input.UserName), Times.Once);
            }

            // check username/password work
            bitbucketApi.Verify(m => m.GetUserInformationAsync(input.UserName, password, false), Times.Once);
        }

        private void VerifyBasicAuthFlowDidNotRun(string password, bool storedAccount, bool expected, InputArguments input, System.Threading.Tasks.Task<ICredential> credential)
        {
            Assert.Equal(expected, credential != null);

            var remoteUri = input.GetRemoteUri();

            // never prompt the user for basic credentials
            bitbucketAuthentication.Verify(m => m.GetBasicCredentialsAsync(remoteUri, input.UserName), !storedAccount ? Times.Once : Times.Never);
        }

        private void VerifyOAuthFlowRan(string password, bool storedAccount, bool expected, InputArguments input, System.Threading.Tasks.Task<ICredential> credential)
        {
            Assert.Equal(expected, credential != null);

            var remoteUri = input.GetRemoteUri();

            if (storedAccount)
            {
                // use refresh token to get new access token and refresh token
                bitbucketAuthentication.Verify(m => m.RefreshOAuthCredentialsAsync(MOCK_REFRESH_TOKEN), Times.Once);

                // check access token works
                bitbucketApi.Verify(m => m.GetUserInformationAsync(null, MOCK_ACCESS_TOKEN, true), Times.Once);
            }
            else
            {
                // prompt user for basic auth
                bitbucketAuthentication.Verify(m => m.GetBasicCredentialsAsync(remoteUri, input.UserName), Times.Once);

                // check if entered Basic Auth credentials work
                bitbucketApi.Verify(m => m.GetUserInformationAsync(input.UserName, password, false), Times.Once);

                // Basic Auth 403-ed so push user through OAuth flow
                bitbucketAuthentication.Verify(m => m.ShowOAuthRequiredPromptAsync(), Times.Once);
            }
        }

        private void VerifyOAuthFlowWasNotRun(string password, bool storedAccount, bool expected, InputArguments input, System.Threading.Tasks.Task<ICredential> credential)
        {
            Assert.Equal(expected, credential != null);

            var remoteUri = input.GetRemoteUri();

            // never prompt user through OAuth flow
            bitbucketAuthentication.Verify(m => m.ShowOAuthRequiredPromptAsync(), Times.Never);

            // Never try to refresh Access Token
            bitbucketAuthentication.Verify(m => m.RefreshOAuthCredentialsAsync(It.IsAny<string>()), Times.Never);

            // never check access token works
            bitbucketApi.Verify(m => m.GetUserInformationAsync(null, MOCK_ACCESS_TOKEN, true), Times.Never);
        }

        private void MockStoredOAuthAccount(TestCommandContext context, InputArguments input)
        {
            // refresh token
            context.CredentialStore.Add("https://bitbucket.org/refresh_token", new TestCredential(input.Host, input.UserName, MOCK_REFRESH_TOKEN));
            // auth token
            context.CredentialStore.Add("https://bitbucket.org", new TestCredential(input.Host, input.UserName, MOCK_ACCESS_TOKEN));
        }

        private void MockRemoteValidRefreshToken()
        {
            bitbucketAuthentication.Setup(m => m.RefreshOAuthCredentialsAsync(MOCK_REFRESH_TOKEN)).ReturnsAsync(new OAuth2TokenResult(MOCK_ACCESS_TOKEN, "access_token"));
        }

        private static void MockInvalidRemoteBasicAccount(Mock<IBitbucketRestApi> bitbucketApi, Mock<IBitbucketAuthentication> bitbucketAuthentication)
        {
            bitbucketAuthentication.Setup(m => m.GetBasicCredentialsAsync(It.IsAny<Uri>(), It.IsAny<String>())).ReturnsAsync((TestCredential)null);

            bitbucketApi.Setup(x => x.GetUserInformationAsync(It.IsAny<String>(), It.IsAny<String>(), false)).ReturnsAsync(new RestApiResult<UserInfo>(System.Net.HttpStatusCode.Unauthorized));

        }
        private static void MockUserEnteredBasicCredentials(Mock<IBitbucketAuthentication> bitbucketAuthentication, InputArguments input, string password)
        {
            var remoteUri = input.GetRemoteUri();
            bitbucketAuthentication.Setup(m => m.GetBasicCredentialsAsync(remoteUri, input.UserName)).ReturnsAsync(new TestCredential(input.Host, input.UserName, password));
        }

        private static void MockUserDidNotEnteredBasicCredentials(Mock<IBitbucketAuthentication> bitbucketAuthentication)
        {
            bitbucketAuthentication.Setup(m => m.GetBasicCredentialsAsync(It.IsAny<Uri>(), It.IsAny<String>())).ReturnsAsync((TestCredential)null);
        }

        private static void MockValidRemoteAccount(Mock<IBitbucketRestApi> bitbucketApi, InputArguments input, string password, bool twoFAEnabled)
        {
            var userInfo = new UserInfo() { IsTwoFactorAuthenticationEnabled = twoFAEnabled };
            bitbucketApi.Setup(x => x.GetUserInformationAsync(input.UserName, password, false)).ReturnsAsync(new RestApiResult<UserInfo>(System.Net.HttpStatusCode.OK, userInfo));
        }

        private static void MockStoredBasicAccount(TestCommandContext context, InputArguments input, string password)
        {
            context.CredentialStore.Add("https://bitbucket.org", new TestCredential(input.Host, input.UserName, password));
        }

        private static void MockValidStoredOAuthUser(TestCommandContext context, Mock<IBitbucketRestApi> bitbucketApi)
        {
            var userInfo = new UserInfo() { IsTwoFactorAuthenticationEnabled = false };
            bitbucketApi.Setup(x => x.GetUserInformationAsync("jsquire", "password1", false)).ReturnsAsync(new RestApiResult<UserInfo>(System.Net.HttpStatusCode.OK, userInfo));
            context.CredentialStore.Add("https://bitbucket.org", new TestCredential("https://bitbucket.org", "jsquire", "password1"));
        }
    }
}
