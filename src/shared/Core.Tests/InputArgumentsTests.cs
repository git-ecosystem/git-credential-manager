using System;
using System.Collections.Generic;
using Xunit;

namespace GitCredentialManager.Tests
{
    public class InputArgumentsTests
    {
        [Fact]
        public void InputArguments_Ctor_Null_ThrowsArgNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new InputArguments((IDictionary<string, string>)null));
            Assert.Throws<ArgumentNullException>(() => new InputArguments((IDictionary<string, IList<string>>)null));
        }

        [Fact]
        public void InputArguments_CommonArguments_ValuePresent_ReturnsValues()
        {
            var dict = new Dictionary<string, IList<string>>
            {
                ["protocol"] = new[] { "https" },
                ["host"]     = new[] { "example.com" },
                ["path"]     = new[] { "an/example/path" },
                ["username"] = new[] { "john.doe" },
                ["password"] = new[] { "password123" },
                ["wwwauth"]  = new[]
                {
                    "basic realm=\"example.com\"",
                    "bearer authorize_uri=https://id.example.com p=1 q=0"
                }
            };

            var inputArgs = new InputArguments(dict);

            Assert.Equal("https",           inputArgs.Protocol);
            Assert.Equal("example.com",     inputArgs.Host);
            Assert.Equal("an/example/path", inputArgs.Path);
            Assert.Equal("john.doe",        inputArgs.UserName);
            Assert.Equal("password123",     inputArgs.Password);
            Assert.Equal(new[]
                {
                    "basic realm=\"example.com\"",
                    "bearer authorize_uri=https://id.example.com p=1 q=0"
                },
                inputArgs.WwwAuth);
        }

        [Fact]
        public void InputArguments_CommonArguments_ValueMissing_ReturnsNullOrEmptyCollection()
        {
            var dict = new Dictionary<string, string>();

            var inputArgs = new InputArguments(dict);

            Assert.Null(inputArgs.Protocol);
            Assert.Null(inputArgs.Host);
            Assert.Null(inputArgs.Path);
            Assert.Null(inputArgs.UserName);
            Assert.Null(inputArgs.Password);
            Assert.Empty(inputArgs.WwwAuth);
        }

        [Fact]
        public void InputArguments_OtherArguments()
        {
            var dict = new Dictionary<string, IList<string>>
            {
                ["foo"] = new[] { "bar" },
                ["multi"] = new[] { "val1", "val2", "val3" },
            };

            var inputArgs = new InputArguments(dict);

            Assert.Equal("bar", inputArgs["foo"]);
            Assert.Equal("bar", inputArgs.GetArgumentOrDefault("foo"));
            Assert.Equal(new[] { "val1", "val2", "val3" }, inputArgs.GetMultiArgumentOrDefault("multi"));
        }

        [Fact]
        public void InputArguments_GetRemoteUri_NoAuthority_ReturnsNull()
        {
            var dict = new Dictionary<string, string>();

            var inputArgs = new InputArguments(dict);

            Uri actualUri = inputArgs.GetRemoteUri();

            Assert.Null(actualUri);
        }

        [Fact]
        public void InputArguments_GetRemoteUri_Authority_ReturnsUriWithAuthority()
        {
            var expectedUri = new Uri("https://example.com/");

            var dict = new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "example.com"
            };

            var inputArgs = new InputArguments(dict);

            Uri actualUri = inputArgs.GetRemoteUri();

            Assert.NotNull(actualUri);
            Assert.Equal(expectedUri, actualUri);
        }

        [Fact]
        public void InputArguments_GetRemoteUri_IncludeUser_Authority_ReturnsUriWithAuthorityAndUser()
        {
            var expectedUri = new Uri("https://john.doe@example.com/");

            var dict = new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "example.com",

                // Username should appear in the returned URI; the password should not
                ["username"] = "john.doe",
                ["password"] = "password123"
            };

            var inputArgs = new InputArguments(dict);

            Uri actualUri = inputArgs.GetRemoteUri(includeUser: true);

            Assert.NotNull(actualUri);
            Assert.Equal(expectedUri, actualUri);
        }

        [Fact]
        public void InputArguments_GetRemoteUri_IncludeUserSpecialCharacters_Authority_ReturnsUriWithAuthorityAndUser()
        {
            var expectedUri = new Uri("https://john.doe%40domain.com@example.com/");

            var dict = new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "example.com",

                // Username should appear in the returned URI; the password should not
                ["username"] = "john.doe@domain.com",
                ["password"] = "password123"
            };

            var inputArgs = new InputArguments(dict);

            Uri actualUri = inputArgs.GetRemoteUri(includeUser: true);

            Assert.NotNull(actualUri);
            Assert.Equal(expectedUri, actualUri);
        }

        [Fact]
        public void InputArguments_GetRemoteUri_IncludeUserNoUser_Authority_ReturnsUriWithAuthority()
        {
            var expectedUri = new Uri("https://example.com/");

            var dict = new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "example.com",
            };

            var inputArgs = new InputArguments(dict);

            Uri actualUri = inputArgs.GetRemoteUri(includeUser: true);

            Assert.NotNull(actualUri);
            Assert.Equal(expectedUri, actualUri);
        }

        [Fact]
        public void InputArguments_GetRemoteUri_AuthorityAndPort_ReturnsUriWithAuthorityAndPort()
        {
            var expectedUri = new Uri("https://example.com:456/");

            var dict = new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "example.com:456"
            };

            var inputArgs = new InputArguments(dict);

            Uri actualUri = inputArgs.GetRemoteUri();

            Assert.NotNull(actualUri);
            Assert.Equal(expectedUri, actualUri);
        }

        [Fact]
        public void InputArguments_GetRemoteUri_AuthorityPath_ReturnsUriWithAuthorityAndPath()
        {
            var expectedUri = new Uri("https://example.com/an/example/path");

            var dict = new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "example.com",
                ["path"]     = "an/example/path"
            };

            var inputArgs = new InputArguments(dict);

            Uri actualUri = inputArgs.GetRemoteUri();

            Assert.NotNull(actualUri);
            Assert.Equal(expectedUri, actualUri);
        }

        [Fact]
        public void InputArguments_GetRemoteUri_AuthorityPathUserInfo_ReturnsUriWithAuthorityAndPath()
        {
            var expectedUri = new Uri("https://example.com/an/example/path");

            var dict = new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "example.com",
                ["path"]     = "an/example/path",

                // Username and password are not expected to appear in the returned URI
                ["username"] = "john.doe",
                ["password"] = "password123"
            };

            var inputArgs = new InputArguments(dict);

            Uri actualUri = inputArgs.GetRemoteUri();

            Assert.NotNull(actualUri);
            Assert.Equal(expectedUri, actualUri);
        }

        [Theory]
        [InlineData("foo?query=true")]
        [InlineData("foo#fragment")]
        [InlineData("foo?query=true#fragment")]
        public void InputArguments_GetRemoteUri_PathQueryFragment_ReturnsCorrectUri(string path)
        {
            var expectedUri = new Uri($"https://example.com/{path}");

            var dict = new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "example.com",
                ["path"]     = path
            };

            var inputArgs = new InputArguments(dict);

            Uri actualUri = inputArgs.GetRemoteUri();

            Assert.NotNull(actualUri);
            Assert.Equal(expectedUri, actualUri);
        }

        [Fact]
        public void InputArguments_GetRemoteUri_IncludeUser_AuthorityPathUserInfo_ReturnsUriWithAll()
        {
            var expectedUri = new Uri("https://john.doe@example.com/an/example/path");

            var dict = new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "example.com",
                ["path"]     = "an/example/path",

                // Username should appear in the returned URI; the password should not
                ["username"] = "john.doe",
                ["password"] = "password123"
            };

            var inputArgs = new InputArguments(dict);

            Uri actualUri = inputArgs.GetRemoteUri(includeUser: true);

            Assert.NotNull(actualUri);
            Assert.Equal(expectedUri, actualUri);
        }

        [Fact]
        public void InputArguments_TryGetHostAndPort_NoPort_ReturnsHostName()
        {
            const string expectedHostName = "example.com";

            var dict = new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "example.com"
            };

            var inputArgs = new InputArguments(dict);

            bool result = inputArgs.TryGetHostAndPort(out string actualHostName, out int? actualPort);

            Assert.True(result);
            Assert.NotNull(actualHostName);
            Assert.Equal(expectedHostName, actualHostName);
            Assert.Null(actualPort);
        }

        [Fact]
        public void InputArguments_TryGetHostAndPort_Port_ReturnsHostNameAndPort()
        {
            const string expectedHostName = "example.com";
            const int expectedPort = 456;

            var dict = new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "example.com:456"
            };

            var inputArgs = new InputArguments(dict);

            bool result = inputArgs.TryGetHostAndPort(out string actualHostName, out int? actualPort);

            Assert.True(result);
            Assert.NotNull(actualHostName);
            Assert.Equal(expectedHostName, actualHostName);
            Assert.NotNull(actualPort);
            Assert.Equal(expectedPort, actualPort);
        }

        [Fact]
        public void InputArguments_TryGetHostAndPort_BadPort_ReturnsFalse()
        {
            var dict = new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "example.com:not-a-port"
            };

            var inputArgs = new InputArguments(dict);

            bool result = inputArgs.TryGetHostAndPort(out _, out int? actualPort);

            Assert.False(result);
            Assert.Null(actualPort);
        }

        [Fact]
        public void InputArguments_TryGetHostAndPort_NoHostNoPort_ReturnsFalse()
        {
            var dict = new Dictionary<string, string>
            {
                ["protocol"] = "https",
            };

            var inputArgs = new InputArguments(dict);

            bool result = inputArgs.TryGetHostAndPort(out _, out _);

            Assert.False(result);
        }
    }
}
