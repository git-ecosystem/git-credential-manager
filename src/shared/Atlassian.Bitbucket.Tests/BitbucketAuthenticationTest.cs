using System;
using System.Threading.Tasks;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace Atlassian.Bitbucket.Tests
{
    public class BitbucketAuthenticationTest
    {
        [Theory]
        [InlineData("jsquire", "password")]
        public async Task BitbucketAuthentication_GetBasicCredentialsAsync_SucceedsAfterUserInput(string username, string password)
        {
            var context = new TestCommandContext();
            context.Terminal.Prompts["Username"] = username;
            context.Terminal.SecretPrompts["Password"] = password;
            System.Uri targetUri = null;

            var bitbucketAuthentication = new BitbucketAuthentication(context);

            var result = await bitbucketAuthentication.GetBasicCredentialsAsync(targetUri, username);

            Assert.NotNull(result);
            Assert.Equal(username, result.Account);
            Assert.Equal(password, result.Password);
        }

        [Fact]
        public async Task BitbucketAuthentication_ShowOAuthRequiredPromptAsync_SucceedsAfterUserInput()
        {
            var context = new TestCommandContext();
            context.Terminal.Prompts["Press enter to continue..."] = " ";

            var bitbucketAuthentication = new BitbucketAuthentication(context);

            var result = await bitbucketAuthentication.ShowOAuthRequiredPromptAsync();

            Assert.True(result);
            Assert.Equal($"Your account has two-factor authentication enabled.{Environment.NewLine}" +
                                           $"To continue you must complete authentication in your web browser.{Environment.NewLine}", context.Terminal.Messages[0].Item1);
        }
    }
}