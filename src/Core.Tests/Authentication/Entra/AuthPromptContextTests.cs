using System.Threading.Tasks;
using GitCredentialManager.Authentication.Entra;
using GitCredentialManager.UI.Controls;
using GitCredentialManager.UI.ViewModels;
using Xunit;

namespace GitCredentialManager.Tests.Authentication.Entra;

public class AuthPromptContextTests
{
    [Fact]
    public async Task Description_NotSet_IsNull()
    {
        // Run on a fresh task so state set by other tests on this thread cannot leak in
        string value = await Task.Run(() => AuthPromptContext.Description);

        Assert.Null(value);
    }

    [Fact]
    public async Task Description_FlowsIntoAwaitedWork()
    {
        await Task.Run(async () =>
        {
            AuthPromptContext.Description = "dev.azure.com/org/project/_git/repo";

            string seen = await Task.Run(async () =>
            {
                await Task.Yield();
                return AuthPromptContext.Description;
            });

            Assert.Equal("dev.azure.com/org/project/_git/repo", seen);
        });
    }

    [Fact]
    public async Task Description_IsIsolatedBetweenConcurrentFlows()
    {
        async Task<string> Flow(string description)
        {
            AuthPromptContext.Description = description;
            await Task.Delay(50);
            return AuthPromptContext.Description;
        }

        string[] seen = await Task.WhenAll(
            Task.Run(() => Flow("host/one")),
            Task.Run(() => Flow("host/two")));

        Assert.Equal(new[] {"host/one", "host/two"}, seen);
    }

    [Fact]
    public async Task DescribeOrigin_WithRequest_ListsRequestAndWorkingDirectory()
    {
        string result = await Task.Run(() =>
        {
            AuthPromptContext.Description = "dev.azure.com/org/project/_git/repo";
            return ProgressWindow.DescribeOrigin();
        });

        Assert.Contains("Sign-in for: dev.azure.com/org/project/_git/repo", result);
        Assert.Contains($"Requested from: {System.Environment.CurrentDirectory}", result);
    }

    [Fact]
    public async Task DescribeOrigin_WithoutRequest_ListsOnlyWorkingDirectory()
    {
        string result = await Task.Run(() =>
        {
            AuthPromptContext.Description = null;
            return ProgressWindow.DescribeOrigin();
        });

        Assert.DoesNotContain("Sign-in for:", result);
        Assert.Equal($"Requested from: {System.Environment.CurrentDirectory}", result);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("dev.azure.com/org", true)]
    public void DefaultAccountViewModel_HasContext_ReflectsContext(string context, bool expected)
    {
        var viewModel = new DefaultAccountViewModel {Context = context};

        Assert.Equal(expected, viewModel.HasContext);
    }

    [Fact]
    public void DefaultAccountViewModel_SettingContext_RaisesHasContextChanged()
    {
        var viewModel = new DefaultAccountViewModel();
        var changed = new System.Collections.Generic.List<string>();
        viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        viewModel.Context = "dev.azure.com/org";

        Assert.Contains(nameof(DefaultAccountViewModel.Context), changed);
        Assert.Contains(nameof(DefaultAccountViewModel.HasContext), changed);
    }
}
