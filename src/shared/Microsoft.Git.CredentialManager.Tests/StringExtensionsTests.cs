// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests
{
    public class StringExtensionsTests
    {
        [Theory]
        [InlineData("true", true)]
        [InlineData("TRUE", true)]
        [InlineData("tRuE", true)]
        [InlineData("yes", true)]
        [InlineData("YES", true)]
        [InlineData("yEs", true)]
        [InlineData("on", true)]
        [InlineData("ON", true)]
        [InlineData("oN", true)]
        [InlineData("1", true)]
        [InlineData("false", false)]
        [InlineData("i am a random string", false)]
        [InlineData("", false)]
        [InlineData("     ", false)]
        [InlineData("\t", false)]
        [InlineData(null, false)]
        public void StringExtensions_IsTruthy(string input, bool expected)
        {
            if (expected)
            {
                Assert.True(StringExtensions.IsTruthy(input));
            }
            else
            {
                Assert.False(StringExtensions.IsTruthy(input));
            }
        }
    }
}
