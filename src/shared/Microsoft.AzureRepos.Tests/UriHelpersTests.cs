using System;
using System.Collections.Generic;
using GitCredentialManager;
using Xunit;

namespace Microsoft.AzureRepos.Tests
{
    public class UriHelpersTests
    {
        [Theory]
        [InlineData("a/b",  "c/d",  "a/b/c/d")]
        [InlineData("a/b/", "c/d",  "a/b/c/d")]
        [InlineData("a/b",  "/c/d", "a/b/c/d")]
        [InlineData("a/b/", "/c/d", "a/b/c/d")]
        public void UriHelpers_CombinePath(string basePath, string path, string expected)
        {
            Assert.Equal(expected, UriHelpers.CombinePath(basePath, path));
        }

        [Theory]
        [InlineData("dev.azure.com", true)]
        [InlineData("myorg.visualstudio.com", true)]
        [InlineData("vs-ssh.myorg.visualstudio.com", true)]
        [InlineData("DEV.AZURE.COM", true)]
        [InlineData("MYORG.VISUALSTUDIO.COM", true)]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("    ", false)]
        [InlineData("testdev.azure.com", false)]
        [InlineData("test.dev.azure.com", false)]
        [InlineData("visualstudio.com", false)]
        [InlineData("testvisualstudio.com", false)]
        public void UriHelpers_IsAzureDevOpsHost(string host, bool expected)
        {
            Assert.Equal(expected, UriHelpers.IsAzureDevOpsHost(host));
        }

        [Theory]
        [InlineData("dev.azure.com", true)]
        [InlineData("myorg.visualstudio.com", false)]
        [InlineData("vs-ssh.myorg.visualstudio.com", false)]
        [InlineData("DEV.AZURE.COM", true)]
        [InlineData("MYORG.VISUALSTUDIO.COM", false)]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("    ", false)]
        [InlineData("testdev.azure.com", false)]
        [InlineData("test.dev.azure.com", false)]
        [InlineData("visualstudio.com", false)]
        [InlineData("testvisualstudio.com", false)]
        public void UriHelpers_IsDevAzureComHost(string host, bool expected)
        {
            Assert.Equal(expected, UriHelpers.IsDevAzureComHost(host));
        }

        [Theory]
        [InlineData("dev.azure.com", false)]
        [InlineData("myorg.visualstudio.com", true)]
        [InlineData("vs-ssh.myorg.visualstudio.com", true)]
        [InlineData("DEV.AZURE.COM", false)]
        [InlineData("MYORG.VISUALSTUDIO.COM", true)]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("    ", false)]
        [InlineData("testdev.azure.com", false)]
        [InlineData("test.dev.azure.com", false)]
        [InlineData("visualstudio.com", false)]
        [InlineData("testvisualstudio.com", false)]
        public void UriHelpers_IsVisualStudioComHost(string host, bool expected)
        {
            Assert.Equal(expected, UriHelpers.IsVisualStudioComHost(host));
        }

        [Fact]
        public void UriHelpers_CreateOrganizationUri_Null_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => UriHelpers.CreateOrganizationUri(null, out _));
        }

        [Fact]
        public void UriHelpers_CreateOrganizationUri_AzureHost_ReturnsCorrectUri()
        {
            var expected = new Uri("https://dev.azure.com/myorg");
            var input =  new Uri("https://dev.azure.com/myorg/myproject/_git/myrepo");
            const string expectedOrg = "myorg";

            Uri actual = UriHelpers.CreateOrganizationUri(input, out string actualOrg);

            Assert.Equal(expected, actual);
            Assert.Equal(expectedOrg, actualOrg);
        }

        [Fact]
        public void UriHelpers_CreateOrganizationUri_AzureHost_WithPort_ReturnsCorrectUri()
        {
            var expected = new Uri("https://dev.azure.com:456/myorg");
            var input = new Uri("https://dev.azure.com:456/myorg/myproject/_git/myrepo");
            const string expectedOrg = "myorg";

            Uri actual = UriHelpers.CreateOrganizationUri(input, out string actualOrg);

            Assert.Equal(expected, actual);
            Assert.Equal(expectedOrg, actualOrg);
        }

        [Fact]
        public void UriHelpers_CreateOrganizationUri_AzureHost_OrgAlsoInUser_PrefersPathOrg()
        {
            var expected = new Uri("https://dev.azure.com/myorg-path");
            var input = new Uri("https://myorg-user@dev.azure.com/myorg-path");
            const string expectedOrg = "myorg-path";

            Uri actual = UriHelpers.CreateOrganizationUri(input, out string actualOrg);

            Assert.Equal(expected, actual);
            Assert.Equal(expectedOrg, actualOrg);
        }

        [Fact]
        public void UriHelpers_CreateOrganizationUri_AzureHost_InputArgsMissingPath_HasUser_UsesUserOrg()
        {
            var expected = new Uri("https://dev.azure.com/myorg-user");
            var input = new Uri("https://myorg-user@dev.azure.com");
            const string expectedOrg = "myorg-user";

            Uri actual = UriHelpers.CreateOrganizationUri(input, out string actualOrg);

            Assert.Equal(expected, actual);
            Assert.Equal(expectedOrg, actualOrg);
        }

        [Fact]
        public void UriHelpers_CreateOrganizationUri_AzureHost_InputArgsMissingPathAndUser_ThrowsException()
        {
            var input = new Uri("https://dev.azure.com");

            Assert.Throws<InvalidOperationException>(() => UriHelpers.CreateOrganizationUri(input, out _));
        }

        [Fact]
        public void UriHelpers_CreateOrganizationUri_VisualStudioHost_ReturnsCorrectUri()
        {
            var expected = new Uri("https://myorg.visualstudio.com");
            var input = new Uri("https://myorg.visualstudio.com");
            const string expectedOrg = "myorg";

            Uri actual = UriHelpers.CreateOrganizationUri(input, out string actualOrg);

            Assert.Equal(expected, actual);
            Assert.Equal(expectedOrg, actualOrg);
        }

        [Fact]
        public void UriHelpers_CreateOrganizationUri_VisualStudioHost_MissingOrgInHost_ThrowsException()
        {
            var input = new Uri("https://visualstudio.com");

            Assert.Throws<InvalidOperationException>(() => UriHelpers.CreateOrganizationUri(input, out _));
        }

        [Fact]
        public void UriHelpers_CreateOrganizationUri_NonAzureDevOpsHost_ThrowsException()
        {
            var input = new Uri("https://example.com");

            Assert.Throws<InvalidOperationException>(() => UriHelpers.CreateOrganizationUri(input, out _));
        }
    }
}
