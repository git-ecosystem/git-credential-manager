using System;
using GitCredentialManager.Interop;
using Xunit;

namespace GitCredentialManager.Tests.Interop
{
    public class U8StringConverterTests
    {
        /*
         * Grinning face with squinting eyes and sweat drop
         *   😅
         *   Codepoint : U+1F605
         *   UTF-16    : 0xD83D 0xDE05
         *   UTF-8     : 0xF0 0x9F 0x98 0x85
         *
         * Latin small letter dotless I
         *   ı
         *   Codepoint : U+0131
         *   UTF-16    : 0x0131
         *   UTF-8     : 0xC4 0xB1
         *
         * Greek capital letter omega
         *   Ω
         *   Codepoint : U+03A9
         *   UTF-16    : 0x03A9
         *   UTF-8     : 0xCE 0xA9
         *
         * Snowman without snow
         *   ⛄
         *   Codepoint : U+26C4
         *   UTF-16    : 0x26C4
         *   UTF-8     : 0xE2 0x9B 0x84
         */

        // "Unicode 😅 ıs awesome! Ω ⛄";
        private const string ComplexString = "Unicode \uD83D\uDE05 \u0131s awesome! \u03A9 \u26C4";
        private static readonly byte[] ComplexUtf8 =
        {
            (byte)'U', (byte)'n', (byte)'i', (byte)'c', (byte)'o', (byte)'d', (byte)'e', (byte)' ',
            (byte)'\u00F0', (byte)'\u009F', (byte)'\u0098', (byte)'\u0085', (byte)' ',
            (byte)'\u00C4', (byte)'\u00B1', (byte)'s', (byte)' ',
            (byte)'a', (byte)'w', (byte)'e', (byte)'s', (byte)'o', (byte)'m', (byte)'e', (byte)'!', (byte)' ',
            (byte)'\u00CE', (byte)'\u00A9', (byte)' ',
            (byte)'\u00E2', (byte)'\u009B', (byte)'\u0084', (byte)'\0'
        };

        private const string SimpleString = "Hello, World!";
        private static readonly byte[] SimpleUtf8 =
        {
            (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o', (byte)',', (byte)' ',
            (byte)'W', (byte)'o', (byte)'r', (byte)'l', (byte)'d', (byte)'!', (byte)'\0'
        };

        private static readonly byte[] NullString = {(byte) '\0'};

        [Fact]
        public void U8StringConverter_ToNative_Null_ReturnsNullPointer()
        {
            IntPtr actual = U8StringConverter.ToNative(null);

            Assert.Equal(IntPtr.Zero, actual);
        }


        [Fact]
        public void U8StringConverter_ToNative_EmptyString_ReturnsNullByte()
        {
            unsafe
            {
                byte* actual = (byte*) U8StringConverter.ToNative(string.Empty);

                AssertCStringEqual(NullString,  actual);
            }
        }

        [Fact]
        public void U8StringConverter_ToNative_SimpleString_ReturnsExpectedBytes()
        {
            unsafe
            {
                byte* actual = (byte*) U8StringConverter.ToNative(SimpleString);

                AssertCStringEqual(SimpleUtf8,  actual);
            }
        }

        [Fact]
        public void U8StringConverter_ToNative_ComplexString_ReturnsExpectedBytes()
        {
            unsafe
            {
                byte* actual = (byte*) U8StringConverter.ToNative(ComplexString);

                AssertCStringEqual(ComplexUtf8,  actual);
            }
        }

        [Fact]
        public void U8StringConverter_ToManaged_Null_ReturnsNull()
        {
            unsafe
            {
                string actual = U8StringConverter.ToManaged(null);

                Assert.Null(actual);
            }
        }

        [Fact]
        public void U8StringConverter_ToManaged_ZeroPtr_ReturnsNull()
        {
            unsafe
            {
                string actual = U8StringConverter.ToManaged((byte*) IntPtr.Zero);

                Assert.Null(actual);
            }
        }

        [Fact]
        public void U8StringConverter_ToManaged_NullByte_ReturnsEmptyString()
        {
            unsafe
            {
                fixed (byte* ptr = NullString)
                {
                    string actual = U8StringConverter.ToManaged(ptr);

                    Assert.Equal(string.Empty, actual);
                }
            }
        }

        [Fact]
        public void U8StringConverter_ToManaged_SimpleString_ReturnsExpectedString()
        {
            unsafe
            {
                fixed (byte* ptr = SimpleUtf8)
                {
                    string actual = U8StringConverter.ToManaged(ptr);

                    Assert.Equal(SimpleString, actual);
                }
            }
        }

        [Fact]
        public void U8StringConverter_ToManaged_ComplexString_ReturnsExpectedString()
        {
            unsafe
            {
                fixed (byte* ptr = ComplexUtf8)
                {
                    string actual = U8StringConverter.ToManaged(ptr);

                    Assert.Equal(ComplexString, actual);
                }
            }
        }

        private static unsafe void AssertCStringEqual(byte[] expected, byte* actual)
        {
            for (int i = 0; i < expected.Length; i++)
            {
                byte actualByte = *(actual + i);

                // Check we don't hit a null terminating byte too soon
                if (i < expected.Length - 1)
                {
                    Assert.NotEqual((byte) '\0', actualByte);
                }

                Assert.Equal(expected[i], actualByte);
            }
        }
    }
}
