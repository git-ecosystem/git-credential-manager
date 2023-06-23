using System;
using System.Collections.Generic;
using Xunit;

namespace GitHub.Tests;

public class GitHubAuthChallengeTests
{
    [Fact]
    public void GitHubAuthChallenge_FromHeaders_CaseInsensitive()
    {
        var headers = new[]
        {
            "BASIC REALM=\"GITHUB\"",
            "basic realm=\"github\"",
            "bAsIc ReAlM=\"gItHuB\"",
        };

        IList<GitHubAuthChallenge> challenges = GitHubAuthChallenge.FromHeaders(headers);
        Assert.Equal(3, challenges.Count);

        foreach (var challenge in challenges)
        {
            Assert.Null(challenge.Domain);
            Assert.Null(challenge.Enterprise);
        }
    }

    [Fact]
    public void GitHubAuthChallenge_FromHeaders_MultipleRealms_ReturnsGitHubOnly()
    {
        var headers = new[]
        {
            "Basic realm=\"contoso\"",
            "Basic realm=\"GitHub\"",
            "Basic realm=\"fabrikam\"",
        };

        IList<GitHubAuthChallenge> challenges = GitHubAuthChallenge.FromHeaders(headers);
        Assert.Single(challenges);

        Assert.Null(challenges[0].Domain);
        Assert.Null(challenges[0].Enterprise);
    }

    [Fact]
    public void GitHubAuthChallenge_FromHeaders_NoMatchingRealms_ReturnsEmpty()
    {
        var headers = new[]
        {
            "Basic realm=\"contoso\"",
            "Basic realm=\"fabrikam\"",
            "Basic realm=\"example\"",
        };

        IList<GitHubAuthChallenge> challenges = GitHubAuthChallenge.FromHeaders(headers);
        Assert.Empty(challenges);
    }

    [Theory]
    [InlineData("Basic realm=\"GitHub\" enterprise_hint=\"contoso-corp\" domain_hint=\"contoso\"", "contoso", "contoso-corp")]
    [InlineData("Basic realm=\"GitHub\" domain_hint=\"contoso\"", "contoso", null)]
    [InlineData("Basic realm=\"GitHub\" enterprise_hint=\"contoso-corp\"", null, "contoso-corp")]
    [InlineData("Basic realm=\"GitHub\" domain_hint=\"fab\" enterprise_hint=\"fabirkamopensource\"", "fab", "fabirkamopensource")]
    [InlineData("Basic enterprise_hint=\"iana\" realm=\"GitHub\" domain_hint=\"example\"", "example", "iana")]
    [InlineData("Basic domain_hint=\"test\" enterprise_hint=\"test-inc\" realm=\"GitHub\"", "test", "test-inc")]
    public void GitHubAuthChallenge_FromHeaders_Hints_ReturnsWithHints(string header, string domain, string enterprise)
    {
        IList<GitHubAuthChallenge> challenges = GitHubAuthChallenge.FromHeaders(new[] { header });
        Assert.Single(challenges);

        Assert.Equal(domain, challenges[0].Domain);
        Assert.Equal(enterprise, challenges[0].Enterprise);
    }

    [Fact]
    public void GitHubAuthChallenge_FromHeaders_EmptyHeaders_ReturnsEmpty()
    {
        string[] headers = Array.Empty<string>();
        IList<GitHubAuthChallenge> challenges = GitHubAuthChallenge.FromHeaders(headers);
        Assert.Empty(challenges);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData("alice", true)]
    [InlineData("alice_contoso", false)]
    [InlineData("alice_CONTOSO", false)]
    [InlineData("alice_contoso_alt", false)]
    [InlineData("pj_nitin", true)]
    [InlineData("up_the_irons", true)]
    public void GitHubAuthChallenge_IsDomainMember_NoHint(string userName, bool expected)
    {
        var challenge = new GitHubAuthChallenge();
        Assert.Equal(expected, challenge.IsDomainMember(userName));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData("alice", false)]
    [InlineData("alice_contoso", true)]
    [InlineData("alice_CONTOSO", true)]
    [InlineData("alice_contoso_alt", false)]
    [InlineData("pj_nitin", false)]
    [InlineData("up_the_irons", false)]
    public void GitHubAuthChallenge_IsDomainMember_DomainHint(string userName, bool expected)
    {
        var realm = new GitHubAuthChallenge("contoso", "contoso-corp");
        Assert.Equal(expected, realm.IsDomainMember(userName));
    }

    [Fact]
    public void GitHubAuthChallenge_Equals_Null_ReturnsFalse()
    {
        var challenge = new GitHubAuthChallenge("contoso", "contoso-corp");
        Assert.False(challenge.Equals(null));
    }

    [Fact]
    public void GitHubAuthChallenge_Equals_SameInstance_ReturnsTrue()
    {
        var challenge = new GitHubAuthChallenge("contoso", "contoso-corp");
        Assert.True(challenge.Equals(challenge));
    }

    [Fact]
    public void GitHubAuthChallenge_Equals_DifferentInstance_ReturnsTrue()
    {
        var challenge1 = new GitHubAuthChallenge("contoso", "constoso-corp");
        var challenge2 = new GitHubAuthChallenge("contoso", "constoso-corp");
        Assert.True(challenge1.Equals(challenge2));
    }

    [Fact]
    public void GitHubAuthChallenge_Equals_DifferentCase_ReturnsTrue()
    {
        var challenge1 = new GitHubAuthChallenge("contoso", "contoso-corp");
        var challenge2 = new GitHubAuthChallenge("CONTOSO", "CONTOSO-CORP");
        Assert.True(challenge1.Equals(challenge2));
    }

    [Fact]
    public void GitHubAuthChallenge_Equals_DifferentShortCode_ReturnsFalse()
    {
        var challenge1 = new GitHubAuthChallenge("contoso", "constoso-corp");
        var challenge2 = new GitHubAuthChallenge("fab", "fabrikamopensource");
        Assert.False(challenge1.Equals(challenge2));
    }
}
