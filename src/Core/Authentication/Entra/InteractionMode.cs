using System;

namespace GitCredentialManager.Authentication.Entra;

public enum InteractionMode
{
    /// <summary>
    /// Use the most appropriate interaction mode for the current environment.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Use an embedded web view for authentication.
    /// </summary>
    EmbeddedWebView,

    /// <summary>
    /// Use the system web view for authentication.
    /// </summary>
    SystemWebView,

    /// <summary>
    /// Use the device code flow for authentication.
    /// </summary>
    DeviceCode
}

public static class InteractionModeExtensions
{
    extension(InteractionMode mode)
    {
        public string GetDisplayName()
        {
            return mode switch
            {
                InteractionMode.Auto => "Auto",
                InteractionMode.EmbeddedWebView => "Embedded web view",
                InteractionMode.SystemWebView => "System web view",
                InteractionMode.DeviceCode => "Device code",
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
        }
    }
}
