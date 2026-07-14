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

    public static MsalParentWindowAdapter Create(object parentWindow, bool createIfMissing = false)
    {
        return new MsalParentWindowAdapter(parentWindow, createIfMissing);
    }

    private MsalParentWindowAdapter(object parentWindow, bool createIfMissing = false)
    {
        _parentWindow = parentWindow;
        _createIfMissing = createIfMissing;
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
            return ProgressWindow.ShowAndGetHandle(_cts.Token);
        }

        // On Windows we can try and get the console window parent handle if that exists
        if (PlatformUtils.IsWindows())
        {
            IntPtr consoleHandle = Kernel32.GetConsoleWindow();
            IntPtr parentHandle = User32.GetAncestor(consoleHandle, GetAncestorFlags.GetRootOwner);

            if (parentHandle != IntPtr.Zero)
            {
                return parentHandle;
            }
        }

        return null;
    }

    public void Dispose()
    {
        // Close and clean up any stub window we may have created
        _cts.Cancel();
    }
}
