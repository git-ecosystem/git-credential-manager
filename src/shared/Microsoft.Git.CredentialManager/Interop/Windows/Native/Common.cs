// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Microsoft.Git.CredentialManager.Interop.Windows.Native
{
    public static class Common
    {
        // https://docs.microsoft.com/en-gb/windows/desktop/Debug/system-error-codes
        public const int OK = 0;
        public const int ERROR_NO_SUCH_LOGON_SESSION = 0x520;
        public const int ERROR_NOT_FOUND = 0x490;
        public const int ERROR_BAD_USERNAME = 0x89A;
        public const int ERROR_INVALID_FLAGS = 0x3EC;
        public const int ERROR_INVALID_PARAMETER = 0x57;

        public static int GetLastError(bool success)
        {
            if (success)
            {
                return OK;
            }

            return Marshal.GetLastWin32Error();
        }

        public static void ThrowIfError(int error, string defaultErrorMessage = null)
        {
            switch (error)
            {
                case OK:
                    return;
                default:
                    // The Win32Exception constructor will automatically get the human-readable
                    // message for the error code.
                    throw new Exception(defaultErrorMessage, new Win32Exception(error));
            }
        }
    }
}
