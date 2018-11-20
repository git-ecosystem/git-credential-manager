using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests
{
    public class InputArgumentsTests
    {
        [Fact]
        public void InputArguments_Ctor_Null_ThrowsArgNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new InputArguments(null));
        }

        [Fact]
        public void InputArguments_CommonArguments_ValuePresent_ReturnsValues()
        {
            var dict = new Dictionary<string, string>
            {
                ["protocol"] = "https",
                ["host"]     = "example.com",
                ["path"]     = "an/example/path",
                ["username"] = "john.doe",
                ["password"] = "password123"
            };

            var inputArgs = new InputArguments(dict);

            Assert.Equal("https",           inputArgs.Protocol);
            Assert.Equal("example.com",     inputArgs.Host);
            Assert.Equal("an/example/path", inputArgs.Path);
            Assert.Equal("john.doe",        inputArgs.UserName);
            Assert.Equal("password123",     inputArgs.Password);
        }

        [Fact]
        public void InputArguments_CommonArguments_ValueMissing_ReturnsNull()
        {
            var dict = new Dictionary<string, string>();

            var inputArgs = new InputArguments(dict);

            Assert.Null(inputArgs.Protocol);
            Assert.Null(inputArgs.Host);
            Assert.Null(inputArgs.Path);
            Assert.Null(inputArgs.UserName);
            Assert.Null(inputArgs.Password);
        }

        [Fact]
        public void InputArguments_OtherArguments()
        {
            var dict = new Dictionary<string, string>
            {
                ["foo"] = "bar"
            };

            var inputArgs = new InputArguments(dict);

            Assert.Equal("bar", inputArgs["foo"]);
            Assert.Equal("bar", inputArgs.GetArgumentOrDefault("foo"));
        }
    }
}
