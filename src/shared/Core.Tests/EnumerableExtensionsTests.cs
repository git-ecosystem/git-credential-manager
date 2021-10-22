using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace GitCredentialManager.Tests
{
    public class EnumerableExtensionsTests
    {
        [Fact]
        public void EnumerableExtensions_ConcatMany_Null_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => EnumerableExtensions.ConcatMany((IEnumerable<object>)null).ToArray());
        }

        [Fact]
        public void EnumerableExtensions_ConcatMany_OneSequence_ReturnsSequence()
        {
            int[] seq1 = {0, 1, 2, 3, 4, 4, 4, 4, 5, 6, 7, 8, 9};
            int[] expectedResult = seq1;

            int[] actualResult = EnumerableExtensions.ConcatMany(seq1).ToArray();

            Assert.Equal(expectedResult, actualResult);
        }

        [Fact]
        public void EnumerableExtensions_ConcatMany_TwoSequences_ReturnsConcatenateSequences()
        {
            int[] seq1 = {0, 1, 2, 3, 4, 4};
            int[] seq2 = {4, 4, 5, 6, 7, 8, 9};
            int[] expectedResult = {0, 1, 2, 3, 4, 4, 4, 4, 5, 6, 7, 8, 9};

            int[] actualResult = EnumerableExtensions.ConcatMany(seq1, seq2).ToArray();

            Assert.Equal(expectedResult, actualResult);
        }

        [Fact]
        public void EnumerableExtensions_ConcatMany_ManySequences_ReturnsConcatenateSequences()
        {
            int[] seq1 = {0, 1};
            int[] seq2 = {2, 3};
            int[] seq3 = {4, 4};
            int[] seq4 = {4, 4};
            int[] seq5 = {5, 6};
            int[] seq6 = {7, 8};
            int[] seq7 = {9};
            int[] expectedResult = {0, 1, 2, 3, 4, 4, 4, 4, 5, 6, 7, 8, 9};

            int[] actualResult = EnumerableExtensions.ConcatMany(seq1, seq2, seq3, seq4, seq5, seq6, seq7).ToArray();

            Assert.Equal(expectedResult, actualResult);
        }
    }
}
