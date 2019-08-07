// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.Tests.Objects;
using Xunit;

namespace Microsoft.Authentication.Helper.Tests
{
    public class ApplicationTests
    {
        [Fact]
        public async Task Application_RunAsync_MissingAuthority_ReturnsError()
        {
            var inputDict = new Dictionary<string, string>
            {
                ["clientId"]    = "test-clientid",
                ["redirectUri"] = "https://test-redirecturi/",
                ["resource"]    = "test-resource",
            };

            var context = new TestCommandContext {Streams = {In = DictionaryToString(inputDict)}};
            var app = new TestApplication(context);

            int exitCode = await app.RunAsync(new string[0]);
            IDictionary<string, string> outputDict = ParseDictionary(context.Streams.Out);

            Assert.Equal(-1, exitCode);
            Assert.True(outputDict.ContainsKey("error"));
        }

        [Fact]
        public async Task Application_RunAsync_MissingClientId_ReturnsError()
        {
            var inputDict = new Dictionary<string, string>
            {
                ["authority"]   = "test-authority",
                ["redirectUri"] = "https://test-redirecturi/",
                ["resource"]    = "test-resource",
            };

            var context = new TestCommandContext {Streams = {In = DictionaryToString(inputDict)}};
            var app = new TestApplication(context);

            int exitCode = await app.RunAsync(new string[0]);
            IDictionary<string, string> outputDict = ParseDictionary(context.Streams.Out);

            Assert.Equal(-1, exitCode);
            Assert.True(outputDict.ContainsKey("error"));
        }

        [Fact]
        public async Task Application_RunAsync_MissingRedirectUri_ReturnsError()
        {
            var inputDict = new Dictionary<string, string>
            {
                ["authority"]   = "test-authority",
                ["clientId"]    = "test-clientid",
                ["resource"]    = "test-resource",
            };

            var context = new TestCommandContext {Streams = {In = DictionaryToString(inputDict)}};
            var app = new TestApplication(context);

            int exitCode = await app.RunAsync(new string[0]);
            IDictionary<string, string> outputDict = ParseDictionary(context.Streams.Out);

            Assert.Equal(-1, exitCode);
            Assert.True(outputDict.ContainsKey("error"));
        }

        [Fact]
        public async Task Application_RunAsync_MissingResource_ReturnsError()
        {
            var inputDict = new Dictionary<string, string>
            {
                ["authority"]   = "test-authority",
                ["clientId"]    = "test-clientid",
                ["redirectUri"] = "https://test-redirecturi/",
            };

            var context = new TestCommandContext {Streams = {In = DictionaryToString(inputDict)}};
            var app = new TestApplication(context);

            int exitCode = await app.RunAsync(new string[0]);
            IDictionary<string, string> outputDict = ParseDictionary(context.Streams.Out);

            Assert.Equal(-1, exitCode);
            Assert.True(outputDict.ContainsKey("error"));
        }

        [Fact]
        public async Task Application_RunAsync_ValidInput_ReturnsAccessToken()
        {
            const string expectedAccessToken = "test-access-token";
            const string expectedAuthority   = "test-authority";
            const string expectedClientId    = "test-clientid";
            const string expectedRedirectUri = "https://test-redirecturi/";
            const string expectedResource    = "test-resource";
            const string expectedRemoteUrl   = "test://remote";

            var inputDict = new Dictionary<string, string>
            {
                ["authority"]   = expectedAuthority,
                ["clientId"]    = expectedClientId,
                ["redirectUri"] = expectedRedirectUri,
                ["resource"]    = expectedResource,
                ["remoteUrl"]   = expectedRemoteUrl,
            };
            var context = new TestCommandContext {Streams = {In = DictionaryToString(inputDict)}};
            var app = new TestApplication(context)
            {
                GetAccessTokenCallback = (authority, clientId, redirectUri, resource) =>
                {
                    Assert.Equal(expectedAuthority, authority);
                    Assert.Equal(expectedClientId, clientId);
                    Assert.Equal(expectedRedirectUri, redirectUri.ToString());
                    Assert.Equal(expectedResource, resource);
                    return expectedAccessToken;
                }
            };

            int exitCode = await app.RunAsync(new string[0]);
            IDictionary<string, string> outputDict = ParseDictionary(context.Streams.Out);

            Assert.Equal(0, exitCode);
            Assert.True(outputDict.ContainsKey("accessToken"));
            Assert.Equal(expectedAccessToken, outputDict["accessToken"]);
        }

        #region Helpers

        private static IDictionary<string, string> ParseDictionary(StringBuilder sb) => ParseDictionary(sb.ToString());

        private static IDictionary<string, string> ParseDictionary(string str) => new StringReader(str).ReadDictionary();

        private static string DictionaryToString(IDictionary<string, string> dict)
        {
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                StreamExtensions.WriteDictionary(writer, dict);
            }
            return sb.ToString();
        }

        #endregion

        private class TestApplication : Application
        {
            public TestApplication(ICommandContext context)
                : base(context) { }

            public Func<string, string, Uri, string, string> GetAccessTokenCallback { get; set; }

            protected override Task<string> GetAccessTokenAsync(string authority, string clientId, Uri redirectUri, string resource)
            {
                return Task.FromResult(GetAccessTokenCallback(authority, clientId, redirectUri, resource) ?? null);
            }
        }
    }
}
