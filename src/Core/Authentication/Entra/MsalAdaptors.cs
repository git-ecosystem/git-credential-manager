using System;
using System.Net.Http;
using System.Threading;
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

    private readonly Func<CancellationToken, IntPtr> _createWindow;

    public static MsalParentWindowAdapter Create(object parentWindow, bool createIfMissing = false)
    {
        return new MsalParentWindowAdapter(parentWindow, createIfMissing, ProgressWindow.ShowAndGetHandle);
    }

    internal MsalParentWindowAdapter(object parentWindow, bool createIfMissing,
        Func<CancellationToken, IntPtr> createWindow)
    {
        EnsureArgument.NotNull(createWindow, nameof(createWindow));

        _parentWindow = parentWindow;
        _createIfMissing = createIfMissing;
        _createWindow = createWindow;
    }

    public object GetWindow()
    {
        // Try to use the parent window we were given (if any)
        if (_parentWindow is IntPtr p && p != IntPtr.Zero)
        {
            return _parentWindow;
        }

        // Create a stub window to use as a parent instead (if the caller requires it)
        if (_createIfMissing)
        {
            return _createWindow(_cts.Token);
        }

        return null;
    }

    public void Dispose()
    {
        // Close and clean up any stub window we may have created
        _cts.Cancel();
    }
}
