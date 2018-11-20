// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using Microsoft.Git.CredentialManager;
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
            Assert.Throws<ArgumentNullException>(() => UriHelpers.CreateOrganizationUri(null));
        }

        [Fact]
        public void UriHelpers_CreateOrganizationUri_InputArgsMissingProtocol_ThrowsException()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["host"] = "dev.azure.com"
            });

            Assert.Throws<InvalidOperationException>(() => UriHelpers.CreateOrganizationUri(input));
        }

        [Fact]
        public void UriHelpers_CreateOrganizationUri_InputArgsMissingHost_ThrowsException()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https"
            });

            Assert.Throws<InvalidOperationException>(() => UriHelpers.CreateOrganizationUri(input));
        }

        [Fact]
        public void UriHelpers_CreateOrganizationUri_AzureHost_ReturnsCorrectUri()
        {
            var expected = new Uri("https://dev.azure.com/myorg");
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "dev.azure.com",
                ["path"]     = "myorg/myproject/_git/myrepo"
            });

            Uri actual = UriHelpers.CreateOrganizationUri(input);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void UriHelpers_CreateOrganizationUri_AzureHost_OrgAlsoInUser_PrefersPathOrg()
        {
            var expected = new Uri("https://dev.azure.com/myorg-path");
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "dev.azure.com",
                ["path"]     = "myorg-path",
                ["username"] = "myorg-user"
            });

            Uri actual = UriHelpers.CreateOrganizationUri(input);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void UriHelpers_CreateOrganizationUri_AzureHost_InputArgsMissingPath_HasUser_UsesUserOrg()
        {
            var expected = new Uri("https://dev.azure.com/myorg-user");
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "dev.azure.com",
                ["username"] = "myorg-user"
            });

            Uri actual = UriHelpers.CreateOrganizationUri(input);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void UriHelpers_CreateOrganizationUri_AzureHost_InputArgsMissingPathAndUser_ThrowsException()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "dev.azure.com"
            });

            Assert.Throws<InvalidOperationException>(() => UriHelpers.CreateOrganizationUri(input));
        }

        [Fact]
        public void UriHelpers_CreateOrganizationUri_VisualStudioHost_ReturnsCorrectUri()
        {
            var expected = new Uri("https://myorg.visualstudio.com/");
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "myorg.visualstudio.com",
            });

            Uri actual = UriHelpers.CreateOrganizationUri(input);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void UriHelpers_CreateOrganizationUri_VisualStudioHost_MissingOrgInHost_ThrowsException()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "visualstudio.com",
            });

            Assert.Throws<InvalidOperationException>(() => UriHelpers.CreateOrganizationUri(input));
        }

        [Fact]
        public void UriHelpers_CreateOrganizationUri_NonAzureDevOpsHost_ThrowsException()
        {
            var input = new InputArguments(new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"] = "example.com",
            });

            Assert.Throws<InvalidOperationException>(() => UriHelpers.CreateOrganizationUri(input));
        }
    }
}
