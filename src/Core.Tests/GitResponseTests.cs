using System.Collections.Generic;
using Xunit;

namespace GitCredentialManager.Tests;

public class GitResponseTests
{
    [Fact]
    public void GitResponse_Constructor_SetsCredential()
    {
        ICredential credential = new GitCredential("alice", "hunter2");

        var response = new GitResponse(credential);

        Assert.Same(credential, response.Credential);
    }

    [Fact]
    public void GitResponse_AdditionalProperties_DefaultsToEmptyDictionary()
    {
        var response = new GitResponse(new GitCredential("alice", "hunter2"));

        Assert.NotNull(response.AdditionalProperties);
        Assert.Empty(response.AdditionalProperties);
    }

    [Fact]
    public void GitResponse_AdditionalProperties_CaseInsensitive()
    {
        // Behavioural contract preserved from the legacy GetCredentialResult shape.
        var response = new GitResponse(new GitCredential("alice", "hunter2"));

        response.AdditionalProperties["NTLM"] = "allow";

        Assert.True(response.AdditionalProperties.TryGetValue("ntlm", out string value));
        Assert.Equal("allow", value);
    }

    [Fact]
    public void GitResponse_AdditionalProperties_CanBeReplaced()
    {
        // Existing in-tree callers (notably GenericHostProvider) assign via
        // object-initializer syntax, so the setter must remain.
        var response = new GitResponse(new GitCredential("alice", "hunter2"))
        {
            AdditionalProperties = new Dictionary<string, string>
            {
                ["ntlm"] = "allow",
            },
        };

        Assert.Single(response.AdditionalProperties);
        Assert.Equal("allow", response.AdditionalProperties["ntlm"]);
    }

    [Fact]
    public void GitResponse_Constructor_NullCredential_Throws()
    {
        // The non-cancelled ctor requires a credential. Use Cancel() for the no-credential case.
        Assert.Throws<System.ArgumentNullException>(() => new GitResponse(null));
    }

    [Fact]
    public void GitResponse_Ok_ReturnsSuccessfulResponseWithCredential()
    {
        ICredential credential = new GitCredential("alice", "hunter2");

        var response = GitResponse.Ok(credential);

        Assert.Same(credential, response.Credential);
        Assert.False(response.IsCancelled);
    }

    [Fact]
    public void GitResponse_Ok_NullCredential_Throws()
    {
        Assert.Throws<System.ArgumentNullException>(() => GitResponse.Ok(null));
    }

    [Fact]
    public void GitResponse_Cancel_ReturnsCancellationResponseWithNoCredential()
    {
        var response = GitResponse.Cancel();

        Assert.True(response.IsCancelled);
        Assert.False(response.IsYielded);
        Assert.Null(response.Credential);
    }

    [Fact]
    public void GitResponse_Yield_ReturnsYieldedResponseWithNoCredential()
    {
        var response = GitResponse.Yield();

        Assert.True(response.IsYielded);
        Assert.False(response.IsCancelled);
        Assert.Null(response.Credential);
    }

    [Fact]
    public void GitResponse_Ok_IsNeitherCancelledNorYielded()
    {
        var response = GitResponse.Ok(new GitCredential("alice", "hunter2"));

        Assert.False(response.IsCancelled);
        Assert.False(response.IsYielded);
    }
}
