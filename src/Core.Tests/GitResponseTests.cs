using System;
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
        Assert.False(response.IsContinue);
    }

    #region Continue

    [Fact]
    public void GitResponse_Continue_ReturnsResponseWithCredentialAndIsContinue()
    {
        ICredential credential = new GitCredential("alice", "hunter2");

        var response = GitResponse.Continue(credential);

        Assert.Same(credential, response.Credential);
        Assert.True(response.IsContinue);
        Assert.False(response.IsCancelled);
        Assert.False(response.IsYielded);
    }

    [Fact]
    public void GitResponse_Continue_NullCredential_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => GitResponse.Continue(null));
    }

    #endregion

    #region State

    [Fact]
    public void GitResponse_Ok_State_StartsEmptyAndIsReadOnlyView()
    {
        var response = GitResponse.Ok(new GitCredential("alice", "hunter2"));

        Assert.NotNull(response.State);
        Assert.Empty(response.State);
        // IReadOnlyDictionary exposes no mutation methods at all.
        Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>(response.State);
    }

    [Fact]
    public void GitResponse_SetState_OnOk_StoresEntry()
    {
        var response = GitResponse.Ok(new GitCredential("alice", "hunter2"));

        response.SetState("github.account", "alice");

        Assert.Single(response.State);
        Assert.Equal("alice", response.State["github.account"]);
    }

    [Fact]
    public void GitResponse_SetState_OnContinue_StoresEntry()
    {
        var response = GitResponse.Continue(new GitCredential("alice", "hunter2"));

        response.SetState("attempt", "1");

        Assert.Equal("1", response.State["attempt"]);
    }

    [Fact]
    public void GitResponse_SetState_OnCancel_IsSilentNoOp()
    {
        // Setting state on a Cancel response makes no semantic sense (no
        // credential is being returned, so there's nothing for state to
        // accompany). The framework silently discards it so providers that
        // build a response speculatively and then switch shape don't have to
        // strip state.
        var response = GitResponse.Cancel();

        response.SetState("k", "v");

        Assert.Empty(response.State);
    }

    [Fact]
    public void GitResponse_SetState_OnYield_IsSilentNoOp()
    {
        var response = GitResponse.Yield();

        response.SetState("k", "v");

        Assert.Empty(response.State);
    }

    [Fact]
    public void GitResponse_SetState_InvalidKey_AlwaysThrows()
    {
        // Validation is a wire-protocol concern: invalid keys/values would
        // corrupt output regardless of which response shape we're attached to.
        // Validation runs before the shape-based no-op so the bug surfaces
        // even on Cancel/Yield.
        var okResponse = GitResponse.Ok(new GitCredential("alice", "hunter2"));
        var cancelResponse = GitResponse.Cancel();

        Assert.Throws<ArgumentException>(() => okResponse.SetState("gcm.foo", "v"));
        Assert.Throws<ArgumentException>(() => okResponse.SetState("k=k", "v"));
        Assert.Throws<ArgumentException>(() => okResponse.SetState(null, "v"));

        Assert.Throws<ArgumentException>(() => cancelResponse.SetState("gcm.foo", "v"));
        Assert.Throws<ArgumentException>(() => cancelResponse.SetState("k=k", "v"));
    }

    [Fact]
    public void GitResponse_SetState_InvalidValue_AlwaysThrows()
    {
        var okResponse = GitResponse.Ok(new GitCredential("alice", "hunter2"));
        var yieldResponse = GitResponse.Yield();

        Assert.Throws<ArgumentException>(() => okResponse.SetState("k", "has\nnewline"));
        Assert.Throws<ArgumentNullException>(() => okResponse.SetState("k", null));

        Assert.Throws<ArgumentException>(() => yieldResponse.SetState("k", "has\nnewline"));
    }

    [Fact]
    public void GitResponse_WithState_ReturnsSameInstanceForChaining()
    {
        var response = GitResponse.Ok(new GitCredential("alice", "hunter2"));

        GitResponse result = response.WithState("k", "v");

        Assert.Same(response, result);
        Assert.Equal("v", response.State["k"]);
    }

    [Fact]
    public void GitResponse_WithState_ChainsMultipleEntries()
    {
        var response = GitResponse.Continue(new GitCredential("alice", "hunter2"))
            .WithState("github.account", "alice")
            .WithState("attempt", "1");

        Assert.Equal(2, response.State.Count);
        Assert.Equal("alice", response.State["github.account"]);
        Assert.Equal("1", response.State["attempt"]);
    }

    [Fact]
    public void GitResponse_WithState_OnCancel_StillReturnsResponseStateRemainsEmpty()
    {
        var response = GitResponse.Cancel().WithState("k", "v");

        Assert.True(response.IsCancelled);
        Assert.Empty(response.State);
    }

    #endregion
}
