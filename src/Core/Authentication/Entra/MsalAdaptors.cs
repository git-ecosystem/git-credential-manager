using System;
using System.Net.Http;
using System.Threading;
using GitCredentialManager.Interop.Windows.Native;
using GitCredentialManager.UI.Controls;
using Microsoft.Identity.Client;

namespace GitCredentialManager.Authentication.Entra;

internal class MsalHttpClientFactoryAdaptor : IMsalHttpClientFactory
{
    private readonly IHttpClientFactory _factory;
    private HttpClient _instance;

    public MsalHttpClientFactoryAdaptor(IHttpClientFactory factory)
    {
        EnsureArgument.NotNull(factory, nameof(factory));

        _factory = factory;
    }

    // MSAL calls this method each time it wants to use an HTTP client.
    // We ensure we only create a single instance to avoid socket exhaustion.
    public HttpClient GetHttpClient() =>
        _instance ??= _factory.CreateClient();
}

internal class MsalParentWindowAdapter : IDisposable
{
    private readonly object _parentWindow;
    private readonly bool _createIfMissing;
    private readonly CancellationTokenSource _cts = new();

    private readonly Func<IntPtr> _getConsoleParentWindow;
    private readonly Func<IntPtr, bool> _isWindowVisible;
    private readonly Func<CancellationToken, IntPtr> _createWindow;

    public static MsalParentWindowAdapter Create(object parentWindow, bool createIfMissing = false)
    {
        return new MsalParentWindowAdapter(parentWindow, createIfMissing,
            GetConsoleParentWindow, User32.IsWindowVisible, ProgressWindow.ShowAndGetHandle);
    }

    internal MsalParentWindowAdapter(object parentWindow, bool createIfMissing,
        Func<IntPtr> getConsoleParentWindow, Func<IntPtr, bool> isWindowVisible,
        Func<CancellationToken, IntPtr> createWindow)
    {
        EnsureArgument.NotNull(getConsoleParentWindow, nameof(getConsoleParentWindow));
        EnsureArgument.NotNull(isWindowVisible, nameof(isWindowVisible));
        EnsureArgument.NotNull(createWindow, nameof(createWindow));

        _parentWindow = parentWindow;
        _createIfMissing = createIfMissing;

        _getConsoleParentWindow = getConsoleParentWindow;
        _isWindowVisible = isWindowVisible;
        _createWindow = createWindow;
    }

    public object GetWindow()
    {
        if (_parentWindow is IntPtr p && p != IntPtr.Zero)
        {
            return _parentWindow;
        }

        // Create a stub window to use as a parent
        if (_createIfMissing)
        {
            return _createWindow(_cts.Token);
        }

        // See if we can use the console window as a parent.
        // We only consider the window if it is valid and visible.
        IntPtr consoleParent = _getConsoleParentWindow();
        if (consoleParent != IntPtr.Zero && _isWindowVisible(consoleParent))
        {
            return consoleParent;
        }

        return null;
    }

    private static IntPtr GetConsoleParentWindow()
    {
        // On Windows we can try and get the console window parent handle if that exists
        if (!PlatformUtils.IsWindows())
        {
            return IntPtr.Zero;
        }

        IntPtr consoleHandle = Kernel32.GetConsoleWindow();

        // When the parent is using ConPTY (pseudo-terminals) the console
        // window may be a fake/stub (as is the case with Windows Terminal).
        // This means we need to walk up the ancestor chain to get the actual
        // root owner (typically the terminal emulator's window).
        return User32.GetAncestor(consoleHandle, GetAncestorFlags.GetRootOwner);
    }

    public void Dispose()
    {
        // Close and clean up any stub window we may have created
        _cts.Cancel();
    }
}
