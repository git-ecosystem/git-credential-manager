// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;

namespace Microsoft.Git.CredentialManager
{
    public static class StringExtensions
    {
        /// <summary>
        /// Check if the string is considered to be 'truthy' (a value considered equivalent to 'true').
        /// </summary>
        /// <remarks>
        /// Git considers several different values to be equivalent to 'true'; we try to be consistent with this
        /// behavior.
        /// <para/>
        /// See the following Git documentation for a list of values considered to be equivalent to 'true':
        /// https://git-scm.com/docs/git-config#git-config-boolean
        /// </remarks>
        /// <param name="str">String value to check.</param>
        /// <returns>True if the value is 'truthy', false otherwise.</returns>
        public static bool IsTruthy(this string str)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(str, bool.TrueString) ||
                   StringComparer.OrdinalIgnoreCase.Equals(str, "1") ||
                   StringComparer.OrdinalIgnoreCase.Equals(str, "on") ||
                   StringComparer.OrdinalIgnoreCase.Equals(str, "yes");
        }
    }
}
