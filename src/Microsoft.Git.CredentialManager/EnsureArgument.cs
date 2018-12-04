// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;

namespace Microsoft.Git.CredentialManager
{
    public static class EnsureArgument
    {
        public static void NotNull<T>(T arg, string name) where T : class
        {
            if (arg is null)
            {
                throw new ArgumentNullException(name);
            }
        }

        public static void NotNullOrEmpty(string arg, string name)
        {
            NotNull(arg, name);

            if (string.IsNullOrEmpty(arg))
            {
                throw new ArgumentException("Argument cannot be empty.", name);
            }
        }

        public static void NotNullOrWhiteSpace(string arg, string name)
        {
            NotNull(arg, name);

            if (string.IsNullOrWhiteSpace(arg))
            {
                throw new ArgumentException("Argument cannot be empty or white space.", name);
            }
        }

        public static void AbsoluteUri(Uri arg, string name)
        {
            NotNull(arg, name);

            if (!arg.IsAbsoluteUri)
            {
                throw new ArgumentException("Argument must be an absolute URI.", name);
            }
        }

        public static void PositiveOrZero(int arg, string name)
        {
            if (arg < 0)
            {
                throw new ArgumentOutOfRangeException(name, "Argument must be positive or zero (non-negative).");
            }
        }

        public static void Positive(int arg, string name)
        {
            if (arg <= 0)
            {
                throw new ArgumentOutOfRangeException(name, "Argument must be positive.");
            }
        }

        public static void NegativeOrZero(int arg, string name)
        {
            if (arg > 0)
            {
                throw new ArgumentOutOfRangeException(name, "Argument must be negative or zero (non-positive).");
            }
        }

        public static void Negative(int arg, string name)
        {
            if (arg >= 0)
            {
                throw new ArgumentOutOfRangeException(name, "Argument must be negative.");
            }
        }
    }
}
