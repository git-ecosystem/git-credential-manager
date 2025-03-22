using System;
using System.Collections.Generic;
using GitCredentialManager.Interop.MacOS.Native;
using static GitCredentialManager.Interop.MacOS.Native.CoreFoundation;

namespace GitCredentialManager.Interop.MacOS;

public class MacOSPreferences
{
    private readonly string _appId;

    public MacOSPreferences(string appId)
    {
        EnsureArgument.NotNull(appId, nameof(appId));

        _appId = appId;
    }

    /// <summary>
    /// Return a <see cref="string"/> typed value from the app preferences.
    /// </summary>
    /// <param name="key">Preference name.</param>
    /// <exception cref="InvalidOperationException">Thrown if the preference is not a string.</exception>
    /// <returns>
    /// <see cref="string"/> or null if the preference with the given key does not exist.
    /// </returns>
    public string GetString(string key)
    {
        return TryGet(key, CFStringToString, out string value)
            ? value
            : null;
    }

    /// <summary>
    /// Return a <see cref="int"/> typed value from the app preferences.
    /// </summary>
    /// <param name="key">Preference name.</param>
    /// <exception cref="InvalidOperationException">Thrown if the preference is not an integer.</exception>
    /// <returns>
    /// <see cref="int"/> or null if the preference with the given key does not exist.
    /// </returns>
    public int? GetInteger(string key)
    {
        return TryGet(key, CFNumberToInt32, out int value)
            ? value
            : null;
    }

    /// <summary>
    /// Return a <see cref="IDictionary{TKey,TValue}"/> typed value from the app preferences.
    /// </summary>
    /// <param name="key">Preference name.</param>
    /// <exception cref="InvalidOperationException">Thrown if the preference is not a dictionary.</exception>
    /// <returns>
    /// <see cref="IDictionary{TKey,TValue}"/> or null if the preference with the given key does not exist.
    /// </returns>
    public IDictionary<string, string> GetDictionary(string key)
    {
        return TryGet(key, CFDictionaryToDictionary, out IDictionary<string, string> value)
            ? value
            : null;
    }

    private bool TryGet<T>(string key, Func<IntPtr, T> converter, out T value)
    {
        IntPtr cfValue = IntPtr.Zero;
        IntPtr keyPtr = IntPtr.Zero;
        IntPtr appIdPtr = CreateAppIdPtr();

        try
        {
            keyPtr = CFStringCreateWithCString(IntPtr.Zero, key, CFStringEncoding.kCFStringEncodingUTF8);
            cfValue = CFPreferencesCopyAppValue(keyPtr, appIdPtr);

            if (cfValue == IntPtr.Zero)
            {
                value = default;
                return false;
            }

            value = converter(cfValue);
            return true;
        }
        finally
        {
            if (cfValue != IntPtr.Zero) CFRelease(cfValue);
            if (keyPtr != IntPtr.Zero) CFRelease(keyPtr);
            if (appIdPtr != IntPtr.Zero) CFRelease(appIdPtr);
        }
    }

    private IntPtr CreateAppIdPtr()
    {
        return CFStringCreateWithCString(IntPtr.Zero, _appId, CFStringEncoding.kCFStringEncodingUTF8);
    }
}
