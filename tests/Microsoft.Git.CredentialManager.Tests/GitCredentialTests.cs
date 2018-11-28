using Xunit;

namespace Microsoft.Git.CredentialManager.Tests
{
    public class GitCredentialTests
    {
        [Fact]
        public void GitCredential_ToBase64String_ComplexUserPass_ReturnsCorrectString()
        {
            const string expected = "aGVsbG8tbXlfbmFtZSBpczpqb2huLmRvZTp0aGlzIWlzQVA0U1NXMFJEOiB3aXRoPyBfbG90cyBvZi8gY2hhcnM=";
            const string testUserName = "hello-my_name is:john.doe";
            const string testPassword = "this!isAP4SSW0RD: with? _lots of/ chars";

            var credential = new GitCredential(testUserName, testPassword);
            string actual = credential.ToBase64String();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GitCredential_ToBase64String_EmptyUserName_ReturnsCorrectString()
        {
            const string expected = "OmxldG1laW4xMjM=";
            const string testUserName = "";
            const string testPassword = "letmein123";

            var credential = new GitCredential(testUserName, testPassword);
            string actual = credential.ToBase64String();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GitCredential_ToBase64String_EmptyPassword_ReturnsCorrectString()
        {
            const string expected = "am9obi5kb2U6";
            const string testUserName = "john.doe";
            const string testPassword = "";

            var credential = new GitCredential(testUserName, testPassword);
            string actual = credential.ToBase64String();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GitCredential_ToBase64String_EmptyCredential_ReturnsCorrectString()
        {
            const string expected = "Og==";
            const string testUserName = "";
            const string testPassword = "";

            var credential = new GitCredential(testUserName, testPassword);
            string actual = credential.ToBase64String();

            Assert.Equal(expected, actual);
        }
    }
}
