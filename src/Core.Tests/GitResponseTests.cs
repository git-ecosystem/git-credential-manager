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
}
