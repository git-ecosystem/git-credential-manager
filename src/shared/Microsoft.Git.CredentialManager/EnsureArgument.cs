using System;

namespace GitCredentialManager
{
    public static class EnsureArgument
    {
        public static void NotNull<T>(T arg, string name)
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

        public static void InRange(int arg, string name, int lower, int upper, bool lowerInclusive = true, bool upperInclusive = true)
        {
            if (lowerInclusive && arg < lower)
            {
                throw new ArgumentOutOfRangeException(name, $"Argument must be greater than or equal to {lower}.");
            }

            if (!lowerInclusive && arg <= lower)
            {
                throw new ArgumentOutOfRangeException(name, $"Argument must be strictly greater than {lower}.");
            }

            if (upperInclusive && arg > upper)
            {
                throw new ArgumentOutOfRangeException(name, $"Argument must be less than or equal to {upper}.");
            }

            if (!upperInclusive && arg >= upper)
            {
                throw new ArgumentOutOfRangeException(name, $"Argument must be strictly less than {upper}.");
            }
        }
    }
}
