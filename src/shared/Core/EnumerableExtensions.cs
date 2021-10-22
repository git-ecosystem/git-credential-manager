using System;
using System.Collections.Generic;
using System.Linq;

namespace GitCredentialManager
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Concatenates multiple sequences.
        /// </summary>
        /// <param name="first">Initial sequence to concatenate other sequences with.</param>
        /// <param name="others">Other sequences to concatenate together with <paramref name="first"/>.</param>
        /// <typeparam name="TSource">Type of the sequence elements.</typeparam>
        /// <returns>Concatenated sequence.</returns>
        public static IEnumerable<TSource> ConcatMany<TSource>(this IEnumerable<TSource> first, params IEnumerable<TSource>[] others)
        {
            IEnumerable<TSource> result = first;

            foreach (IEnumerable<TSource> other in others)
            {
                result = result.Concat(other);
            }

            return result;
        }

        public static bool TryGetFirst<TSource>(this IEnumerable<TSource> collection, Func<TSource, bool> predicate, out TSource result)
        {
            result = collection.FirstOrDefault(predicate);
            return !(result is null);
        }
    }
}
