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
    [InlineData(-1, false)]
    [InlineData(-1, true)]
    public void GetWindow_ExplicitParent_TakesPrecedence(int parentValue, bool createIfMissing)
    {
        object parentWindow = new IntPtr(parentValue);
        using var adapter = new MsalParentWindowAdapter(parentWindow, createIfMissing,
            () => throw new InvalidOperationException("Console lookup should not be called."),
            _ => throw new InvalidOperationException("Visibility should not be checked."),
            _ => throw new InvalidOperationException("A progress window should not be created."));

        object result = adapter.GetWindow();
        Assert.Same(parentWindow, result);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetWindow_VisibleConsoleRoot_PrecedesProgressWindow(bool createIfMissing)
    {
        var consoleRoot = new IntPtr(42);
        bool visibilityChecked = false;
        using var adapter = new MsalParentWindowAdapter(IntPtr.Zero, createIfMissing,
            () => consoleRoot,
            hwnd =>
            {
                Assert.Equal(consoleRoot, hwnd);
                visibilityChecked = true;
                return true;
            },
            _ => throw new InvalidOperationException("A progress window should not be created."));

        IntPtr result = Assert.IsType<IntPtr>(adapter.GetWindow());

        Assert.Equal(consoleRoot, result);
        Assert.True(visibilityChecked);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(42)]
    public void GetWindow_NoVisibleConsoleRoot_CreatesProgressWhenRequired(int consoleRootValue)
    {
        var consoleRoot = new IntPtr(consoleRootValue);
        var progressWindow = new IntPtr(84);
        int visibilityChecks = 0;
        var windowToken = CancellationToken.None;
        using (var adapter = new MsalParentWindowAdapter(IntPtr.Zero, true,
            () => consoleRoot,
            hwnd =>
            {
                Assert.Equal(consoleRoot, hwnd);
                visibilityChecks++;
                return false;
            },
            ct =>
            {
                windowToken = ct;
                return progressWindow;
            }))
        {
            IntPtr result = Assert.IsType<IntPtr>(adapter.GetWindow());

            Assert.Equal(progressWindow, result);
            Assert.Equal(consoleRootValue == 0 ? 0 : 1, visibilityChecks);
            Assert.True(windowToken.CanBeCanceled);
            Assert.False(windowToken.IsCancellationRequested);
        }

        Assert.True(windowToken.IsCancellationRequested);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(42)]
    public void GetWindow_NoVisibleConsoleRoot_ReturnsNullWhenParentNotRequired(int consoleRootValue)
    {
        var consoleRoot = new IntPtr(consoleRootValue);
        int visibilityChecks = 0;
        using var adapter = new MsalParentWindowAdapter(IntPtr.Zero, false,
            () => consoleRoot,
            hwnd =>
            {
                Assert.Equal(consoleRoot, hwnd);
                visibilityChecks++;
                return false;
            },
            _ => throw new InvalidOperationException("A progress window should not be created."));

        object result = adapter.GetWindow();

        Assert.Null(result);
        Assert.Equal(consoleRootValue == 0 ? 0 : 1, visibilityChecks);
    }

    [Fact]
    public void GetWindow_VisibilityCheckThrows_PropagatesException()
    {
        var exception = new InvalidOperationException("Visibility check failed.");
        using var adapter = new MsalParentWindowAdapter(IntPtr.Zero, true,
            () => new IntPtr(42),
            _ => throw exception,
            _ => throw new InvalidOperationException("A progress window should not be created."));

        var thrown = Assert.Throws<InvalidOperationException>(adapter.GetWindow);
        Assert.Same(exception, thrown);
    }

    [Fact]
    public void Create_ExplicitParent_IsReturned()
    {
        object parentWindow = new IntPtr(42);
        using var adapter = MsalParentWindowAdapter.Create(parentWindow, true);

        object result = adapter.GetWindow();
        Assert.Same(parentWindow, result);
    }

    [PosixFact]
    public void Create_NoExplicitParent_Posix_ReturnsNull()
    {
        using var adapter = MsalParentWindowAdapter.Create(IntPtr.Zero);

        object result = adapter.GetWindow();
        Assert.Null(result);
    }
}
