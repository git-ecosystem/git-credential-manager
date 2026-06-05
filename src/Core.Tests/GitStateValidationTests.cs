using System;
using System.Collections.Generic;
using Xunit;

namespace GitCredentialManager.Tests;

public class GitStateValidationTests
{
    #region IsValidKey

    [Theory]
    [InlineData("github.account")]
    [InlineData("plain")]
    [InlineData("a.b.c.d")]
    [InlineData("with-dashes")]
    [InlineData("with_under")]
    [InlineData("UPPER")]
    public void IsValidKey_ValidKeys_ReturnsTrue(string key)
    {
        Assert.True(GitStateValidation.IsValidKey(key));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("has=equals")]
    [InlineData("has\nnewline")]
    [InlineData("has\0null")]
    [InlineData("gcm.alreadyprefixed")]
    public void IsValidKey_InvalidKeys_ReturnsFalse(string key)
    {
        Assert.False(GitStateValidation.IsValidKey(key));
    }

    #endregion

    #region IsValidValue

    [Theory]
    [InlineData("")]
    [InlineData("alice")]
    [InlineData("value with spaces")]
    [InlineData("value=with=equals")] // values may contain '='; only keys may not
    public void IsValidValue_ValidValues_ReturnsTrue(string value)
    {
        Assert.True(GitStateValidation.IsValidValue(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("has\nnewline")]
    [InlineData("has\0null")]
    public void IsValidValue_InvalidValues_ReturnsFalse(string value)
    {
        Assert.False(GitStateValidation.IsValidValue(value));
    }

    #endregion

    #region ValidateKey

    [Fact]
    public void ValidateKey_NullOrEmpty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => GitStateValidation.ValidateKey(null));
        Assert.Throws<ArgumentException>(() => GitStateValidation.ValidateKey(""));
    }

    [Fact]
    public void ValidateKey_ReservedPrefix_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => GitStateValidation.ValidateKey("gcm.foo"));
        Assert.Contains("gcm.", ex.Message);
    }

    [Theory]
    [InlineData("a=b")]
    [InlineData("a\nb")]
    [InlineData("a\0b")]
    public void ValidateKey_BannedCharacters_ThrowsArgumentException(string key)
    {
        Assert.Throws<ArgumentException>(() => GitStateValidation.ValidateKey(key));
    }

    #endregion

    #region ValidateValue

    [Fact]
    public void ValidateValue_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => GitStateValidation.ValidateValue(null));
    }

    [Theory]
    [InlineData("a\nb")]
    [InlineData("a\0b")]
    public void ValidateValue_BannedCharacters_ThrowsArgumentException(string value)
    {
        Assert.Throws<ArgumentException>(() => GitStateValidation.ValidateValue(value));
    }

    #endregion
}
