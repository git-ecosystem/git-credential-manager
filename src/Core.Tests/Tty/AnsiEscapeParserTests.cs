using System;
using System.Collections.Generic;
using System.Text;
using GitCredentialManager.Tty;
using Xunit;

namespace GitCredentialManager.Tests.Tty;

public class AnsiEscapeParserTests
{
    [Fact]
    public void ReadKey_PrintableAscii_ReturnsCharAndKey()
    {
        var key = Parse("a");
        Assert.Equal('a', key.KeyChar);
        Assert.Equal(ConsoleKey.A, key.Key);
        Assert.False(key.Modifiers.HasFlag(ConsoleModifiers.Shift));
    }

    [Fact]
    public void ReadKey_UppercaseAscii_HasShiftFlag()
    {
        var key = Parse("A");
        Assert.Equal('A', key.KeyChar);
        Assert.Equal(ConsoleKey.A, key.Key);
        Assert.True(key.Modifiers.HasFlag(ConsoleModifiers.Shift));
    }

    [Fact]
    public void ReadKey_Digit_ReturnsDigitKey()
    {
        var key = Parse("7");
        Assert.Equal('7', key.KeyChar);
        Assert.Equal(ConsoleKey.D7, key.Key);
    }

    [Fact]
    public void ReadKey_Space_ReturnsSpacebar()
    {
        Assert.Equal(ConsoleKey.Spacebar, Parse(" ").Key);
    }

    [Theory]
    [InlineData("\r", ConsoleKey.Enter)]
    [InlineData("\n", ConsoleKey.Enter)]
    [InlineData("\t", ConsoleKey.Tab)]
    [InlineData("\x7F", ConsoleKey.Backspace)]
    [InlineData("\x08", ConsoleKey.Backspace)]
    public void ReadKey_ControlChars_MapToNamedKeys(string input, ConsoleKey expected)
    {
        Assert.Equal(expected, Parse(input).Key);
    }

    [Fact]
    public void ReadKey_CtrlLetter_SetsControlFlag()
    {
        var key = Parse("\x01"); // Ctrl+A
        Assert.Equal(ConsoleKey.A, key.Key);
        Assert.True(key.Modifiers.HasFlag(ConsoleModifiers.Control));
    }

    [Theory]
    [InlineData("\x1b[A", ConsoleKey.UpArrow)]
    [InlineData("\x1b[B", ConsoleKey.DownArrow)]
    [InlineData("\x1b[C", ConsoleKey.RightArrow)]
    [InlineData("\x1b[D", ConsoleKey.LeftArrow)]
    [InlineData("\x1b[H", ConsoleKey.Home)]
    [InlineData("\x1b[F", ConsoleKey.End)]
    public void ReadKey_CsiArrowsAndCursor_Mapped(string input, ConsoleKey expected)
    {
        Assert.Equal(expected, Parse(input).Key);
    }

    [Theory]
    [InlineData("\x1b[2~", ConsoleKey.Insert)]
    [InlineData("\x1b[3~", ConsoleKey.Delete)]
    [InlineData("\x1b[5~", ConsoleKey.PageUp)]
    [InlineData("\x1b[6~", ConsoleKey.PageDown)]
    [InlineData("\x1b[1~", ConsoleKey.Home)]
    [InlineData("\x1b[4~", ConsoleKey.End)]
    [InlineData("\x1b[15~", ConsoleKey.F5)]
    [InlineData("\x1b[24~", ConsoleKey.F12)]
    public void ReadKey_CsiTildeKeys_Mapped(string input, ConsoleKey expected)
    {
        Assert.Equal(expected, Parse(input).Key);
    }

    [Fact]
    public void ReadKey_ShiftArrow_SetsShiftModifier()
    {
        // xterm modifier encoding: CSI 1;2 A  → Shift+Up
        var key = Parse("\x1b[1;2A");
        Assert.Equal(ConsoleKey.UpArrow, key.Key);
        Assert.True(key.Modifiers.HasFlag(ConsoleModifiers.Shift));
        Assert.False(key.Modifiers.HasFlag(ConsoleModifiers.Control));
        Assert.False(key.Modifiers.HasFlag(ConsoleModifiers.Alt));
    }

    [Fact]
    public void ReadKey_CtrlArrow_SetsControlModifier()
    {
        // CSI 1;5 A  → Ctrl+Up
        var key = Parse("\x1b[1;5A");
        Assert.Equal(ConsoleKey.UpArrow, key.Key);
        Assert.True(key.Modifiers.HasFlag(ConsoleModifiers.Control));
        Assert.False(key.Modifiers.HasFlag(ConsoleModifiers.Shift));
    }

    [Fact]
    public void ReadKey_ShiftCtrlArrow_SetsBothModifiers()
    {
        // CSI 1;6 A  → Shift+Ctrl+Up
        var key = Parse("\x1b[1;6A");
        Assert.Equal(ConsoleKey.UpArrow, key.Key);
        Assert.True(key.Modifiers.HasFlag(ConsoleModifiers.Shift));
        Assert.True(key.Modifiers.HasFlag(ConsoleModifiers.Control));
    }

    [Fact]
    public void ReadKey_BackTab_ReturnsTabWithShift()
    {
        var key = Parse("\x1b[Z");
        Assert.Equal(ConsoleKey.Tab, key.Key);
        Assert.True(key.Modifiers.HasFlag(ConsoleModifiers.Shift));
    }

    [Theory]
    [InlineData("\x1bOA", ConsoleKey.UpArrow)]
    [InlineData("\x1bOB", ConsoleKey.DownArrow)]
    [InlineData("\x1bOP", ConsoleKey.F1)]
    [InlineData("\x1bOS", ConsoleKey.F4)]
    public void ReadKey_Ss3Sequences_Mapped(string input, ConsoleKey expected)
    {
        Assert.Equal(expected, Parse(input).Key);
    }

    [Fact]
    public void ReadKey_EscapeAlone_TimesOutAsEscape()
    {
        // The blocking source returns one ESC, the timed reader returns -1 on
        // the next call (simulating no follow-up byte arriving within the window).
        var bytes = new Queue<int>(new[] { 0x1B });
        ConsoleKeyInfo? result = AnsiEscapeParser.ReadKey(
            readByte: () => bytes.Count > 0 ? bytes.Dequeue() : -1,
            readByteWithTimeout: () => -1);

        Assert.NotNull(result);
        Assert.Equal(ConsoleKey.Escape, result.Value.Key);
    }

    [Fact]
    public void ReadKey_EmptyStream_ReturnsNull()
    {
        ConsoleKeyInfo? result = AnsiEscapeParser.ReadKey(() => -1, () => -1);
        Assert.Null(result);
    }

    [Fact]
    public void ReadKey_Utf8MultiByte_ReturnsDecodedChar()
    {
        // U+00E9 (é) = 0xC3 0xA9 in UTF-8
        var key = Parse("\u00e9");
        Assert.Equal('\u00e9', key.KeyChar);
    }

    // ----- helpers -----

    private static ConsoleKeyInfo Parse(string utf8Input)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(utf8Input);
        var queue = new Queue<int>();
        foreach (byte b in bytes) queue.Enqueue(b);

        ConsoleKeyInfo? result = AnsiEscapeParser.ReadKey(
            readByte: () => queue.Count > 0 ? queue.Dequeue() : -1,
            readByteWithTimeout: () => queue.Count > 0 ? queue.Dequeue() : -1);

        Assert.NotNull(result);
        return result.Value;
    }
}

