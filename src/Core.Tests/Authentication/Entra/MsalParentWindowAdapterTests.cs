using System;
using System.Threading;
using GitCredentialManager.Authentication.Entra;
using Xunit;

namespace GitCredentialManager.Tests.Authentication.Entra;

public class MsalParentWindowAdapterTests
{
    [Theory]
    [InlineData(42, false)]
    [InlineData(42, true)]
    public void GetWindow_ExplicitParent_ReturnsParent(int parentValue, bool createIfMissing)
    {
        object parentWindow = new IntPtr(parentValue);
        using var adapter = new MsalParentWindowAdapter(parentWindow, createIfMissing,
            _ => throw new InvalidOperationException("A progress window should not be created."));

        object result = adapter.GetWindow();
        Assert.Same(parentWindow, result);
    }

    [Fact]
    public void GetWindow_NoParent_Required_CreatesProgressWindow()
    {
        var progressWindow = new IntPtr(42);
        var windowToken = CancellationToken.None;
        using (var adapter = new MsalParentWindowAdapter(IntPtr.Zero, true,
            ct =>
            {
                windowToken = ct;
                return progressWindow;
            }))
        {
            IntPtr result = Assert.IsType<IntPtr>(adapter.GetWindow());

            Assert.Equal(progressWindow, result);
            Assert.True(windowToken.CanBeCanceled);
            Assert.False(windowToken.IsCancellationRequested);
        }

        Assert.True(windowToken.IsCancellationRequested);
    }

    [Fact]
    public void GetWindow_NoParent_NotRequired_ReturnsNull()
    {
        using var adapter = new MsalParentWindowAdapter(IntPtr.Zero, false,
            _ => throw new InvalidOperationException("A progress window should not be created."));

        object result = adapter.GetWindow();

        Assert.Null(result);
    }
}
