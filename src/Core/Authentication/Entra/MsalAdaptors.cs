using System;
using System.Net.Http;
using GitCredentialManager.Interop.Windows.Native;
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

internal class MsalParentWindowAdapter
{
    private readonly object _parentWindow;

    public static MsalParentWindowAdapter Create(object parentWindow)
    {
        return new MsalParentWindowAdapter(parentWindow);
    }

    private MsalParentWindowAdapter(object parentWindow)
    {
        _parentWindow = parentWindow;
    }

    public object GetWindow()
    {
        if (_parentWindow is IntPtr p && p != IntPtr.Zero)
        {
            return _parentWindow;
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
}
