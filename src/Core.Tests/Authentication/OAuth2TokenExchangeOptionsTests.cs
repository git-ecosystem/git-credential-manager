using System;
using System.Collections.Generic;
using System.Linq;
using GitCredentialManager.Authentication.OAuth;
using Xunit;

namespace GitCredentialManager.Tests.Authentication;

public class OAuth2TokenExchangeOptionsTests
{
    private const string TestSubjectToken = "subject-token";
    private const string TestActorToken = "actor-token";

    private static readonly OAuth2TokenExchangeOptions MinimalOptions = new()
    {
        SubjectToken = TestSubjectToken,
        SubjectTokenType = OAuth2TokenType.AccessToken,
    };

    [Fact]
    public void OAuth2TokenExchangeOptions_IsValid_MinimalOptions_ReturnsTrue()
    {
        Assert.True(MinimalOptions.IsValid(out string error));
        Assert.Null(error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void OAuth2TokenExchangeOptions_IsValid_MissingSubjectToken_ReturnsFalse(string subjectToken)
    {
        OAuth2TokenExchangeOptions options = MinimalOptions with { SubjectToken = subjectToken };

        Assert.False(options.IsValid(out string error));
        Assert.NotNull(error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void OAuth2TokenExchangeOptions_IsValid_InvalidSubjectTokenType_ReturnsFalse(string identifier)
    {
        OAuth2TokenExchangeOptions options = MinimalOptions with { SubjectTokenType = new OAuth2TokenType(identifier) };

        Assert.False(options.IsValid(out string error));
        Assert.NotNull(error);
    }

    [Fact]
    public void OAuth2TokenExchangeOptions_IsValid_ActorTokenTypeWithoutActorToken_ReturnsFalse()
    {
        OAuth2TokenExchangeOptions options = MinimalOptions with { ActorTokenType = OAuth2TokenType.IdToken };

        Assert.False(options.IsValid(out string error));
        Assert.NotNull(error);
    }

    [Fact]
    public void OAuth2TokenExchangeOptions_IsValid_ActorTokenWithoutActorTokenType_ReturnsFalse()
    {
        OAuth2TokenExchangeOptions options = MinimalOptions with { ActorToken = TestActorToken };

        Assert.False(options.IsValid(out string error));
        Assert.NotNull(error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void OAuth2TokenExchangeOptions_IsValid_InvalidActorTokenType_ReturnsFalse(string identifier)
    {
        OAuth2TokenExchangeOptions options = MinimalOptions with
        {
            ActorToken = TestActorToken,
            ActorTokenType = new OAuth2TokenType(identifier),
        };

        Assert.False(options.IsValid(out string error));
        Assert.NotNull(error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void OAuth2TokenExchangeOptions_IsValid_InvalidRequestedTokenType_ReturnsFalse(string identifier)
    {
        OAuth2TokenExchangeOptions options = MinimalOptions with { RequestedTokenType = new OAuth2TokenType(identifier) };

        Assert.False(options.IsValid(out string error));
        Assert.NotNull(error);
    }

    [Fact]
    public void OAuth2TokenExchangeOptions_IsValid_RelativeResource_ReturnsFalse()
    {
        OAuth2TokenExchangeOptions options = MinimalOptions with
        {
            Resources = [new Uri("/api", UriKind.Relative)]
        };

        Assert.False(options.IsValid(out string error));
        Assert.NotNull(error);
    }

    [Fact]
    public void OAuth2TokenExchangeOptions_IsValid_ResourceWithFragment_ReturnsFalse()
    {
        OAuth2TokenExchangeOptions options = MinimalOptions with
        {
            Resources = [new Uri("https://example.com/api#fragment")]
        };

        Assert.False(options.IsValid(out string error));
        Assert.NotNull(error);
    }

    [Fact]
    public void OAuth2TokenExchangeOptions_IsValid_NullResource_ReturnsFalse()
    {
        OAuth2TokenExchangeOptions options = MinimalOptions with
        {
            Resources = [new Uri("https://example.com/api"), null]
        };

        Assert.False(options.IsValid(out string error));
        Assert.NotNull(error);
    }

    [Fact]
    public void OAuth2TokenExchangeOptions_ToRequestParameters_MinimalOptions_ReturnsRequiredParameters()
    {
        var expected = new Dictionary<string, string>
        {
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:token-exchange",
            ["subject_token"] = TestSubjectToken,
            ["subject_token_type"] = "urn:ietf:params:oauth:token-type:access_token",
        };

        AssertParameters(expected, MinimalOptions.ToRequestParameters());
    }

    [Fact]
    public void OAuth2TokenExchangeOptions_ToRequestParameters_NullCollections_ReturnsRequiredParameters()
    {
        OAuth2TokenExchangeOptions options = MinimalOptions with
        {
            Scopes = null,
            Audiences = null,
            Resources = null,
        };

        AssertParameters(MinimalOptions.ToRequestParameters(), options.ToRequestParameters());
    }

    [Fact]
    public void OAuth2TokenExchangeOptions_ToRequestParameters_AllOptions_ReturnsAllParameters()
    {
        var customTokenType = new OAuth2TokenType("urn:example:params:oauth:token-type:custom");

        OAuth2TokenExchangeOptions options = MinimalOptions with
        {
            SubjectTokenType = OAuth2TokenType.JwtToken,
            Scopes = ["read", "write"],
            Audiences = ["audience1", "audience2"],
            Resources = [new Uri("https://example.com/api1"), new Uri("https://example.com/api2")],
            ActorToken = TestActorToken,
            ActorTokenType = OAuth2TokenType.IdToken,
            RequestedTokenType = customTokenType,
        };

        var expected = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "urn:ietf:params:oauth:grant-type:token-exchange"),
            new("subject_token", TestSubjectToken),
            new("subject_token_type", "urn:ietf:params:oauth:token-type:jwt"),
            new("scope", "read write"),
            new("audience", "audience1"),
            new("audience", "audience2"),
            new("resource", "https://example.com/api1"),
            new("resource", "https://example.com/api2"),
            new("actor_token", TestActorToken),
            new("actor_token_type", "urn:ietf:params:oauth:token-type:id_token"),
            new("requested_token_type", customTokenType.Identifier),
        };

        AssertParameters(expected, options.ToRequestParameters());
    }

    [Fact]
    public void OAuth2TokenExchangeOptions_ToRequestParameters_InvalidOptions_ThrowsException()
    {
        OAuth2TokenExchangeOptions options = MinimalOptions with { SubjectToken = null };

        Assert.Throws<InvalidOperationException>(options.ToRequestParameters);
    }

    private static void AssertParameters(
        IEnumerable<KeyValuePair<string, string>> expected, IEnumerable<KeyValuePair<string, string>> actual)
    {
        // Form parameter order is not significant, but repeated parameters must all be present.
        Assert.Equal(
            expected.OrderBy(x => x.Key, StringComparer.Ordinal).ThenBy(x => x.Value, StringComparer.Ordinal),
            actual.OrderBy(x => x.Key, StringComparer.Ordinal).ThenBy(x => x.Value, StringComparer.Ordinal));
    }
}
