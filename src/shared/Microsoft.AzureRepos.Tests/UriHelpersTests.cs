// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
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

        [Fact]
        public void UriHelpers_CreateOrganizationUri_Null_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => UriHelpers.CreateOrganizationUri(null, out _));
        }

        [Fact]
        public void UriHelpers_CreateOrganizationUri_AzureHost_ReturnsCorrectUriAndOrgName()
        {
            const string expectedOrgName = "myorg";
            var expectedOrgUri = new Uri("https://dev.azure.com/myorg");
            var remoteUri = new Uri("https://dev.azure.com/myorg/myproject/_git/myrepo");

            Uri actualOrgUri = UriHelpers.CreateOrganizationUri(remoteUri, out string actualOrgName);

            Assert.Equal(expectedOrgUri, actualOrgUri);
            Assert.Equal(expectedOrgName, actualOrgName);
        }

        [Fact]
        public void UriHelpers_CreateOrganizationUri_AzureHost_OrgAlsoInUser_PrefersPathOrg()
        {
            const string expectedOrgName = "myorg-path";
            var expectedOrgUri = new Uri("https://dev.azure.com/myorg-path");
            var remoteUri = new Uri("https://myorg-user@dev.azure.com/myorg-path");

            Uri actualOrgUri = UriHelpers.CreateOrganizationUri(remoteUri, out string actualOrgName);

            Assert.Equal(expectedOrgUri, actualOrgUri);
            Assert.Equal(expectedOrgName, actualOrgName);
        }

        [Fact]
        public void UriHelpers_CreateOrganizationUri_AzureHost_InputArgsMissingPath_HasUser_UsesUserOrg()
        {
            const string expectedOrgName = "myorg-user";
            var expectedOrgUri = new Uri("https://dev.azure.com/myorg-user");
            var remoteUri = new Uri("https://myorg-user@dev.azure.com");

            Uri actualOrgUri = UriHelpers.CreateOrganizationUri(remoteUri, out string actualOrgName);

            Assert.Equal(expectedOrgUri, actualOrgUri);
            Assert.Equal(expectedOrgName, actualOrgName);
        }

        [Fact]
        public void UriHelpers_CreateOrganizationUri_AzureHost_InputArgsMissingPathAndUser_ThrowsException()
        {
            var remoteUri = new Uri("https://dev.azure.com");

            Assert.Throws<InvalidOperationException>(() => UriHelpers.CreateOrganizationUri(remoteUri, out _));
        }

        [Fact]
        public void UriHelpers_CreateOrganizationUri_VisualStudioHost_ReturnsCorrectUri()
        {
            const string expectedOrgName = "myorg";
            var expectedOrgUri = new Uri("https://myorg.visualstudio.com/");
            var remoteUri = new Uri("https://myorg.visualstudio.com");

            Uri actualOrgUri = UriHelpers.CreateOrganizationUri(remoteUri, out string actualOrgName);

            Assert.Equal(expectedOrgUri, actualOrgUri);
            Assert.Equal(expectedOrgName, actualOrgName);
        }

        [Fact]
        public void UriHelpers_CreateOrganizationUri_VisualStudioHost_MissingOrgInHost_ThrowsException()
        {
            var remoteUri = new Uri("https://visualstudio.com");

            Assert.Throws<InvalidOperationException>(() => UriHelpers.CreateOrganizationUri(remoteUri, out _));
        }

        [Fact]
        public void UriHelpers_CreateOrganizationUri_NonAzureDevOpsHost_ThrowsException()
        {
            var remoteUri = new Uri("https://example.com");

            Assert.Throws<InvalidOperationException>(() => UriHelpers.CreateOrganizationUri(remoteUri, out _));
        }
    }
}
