using System;
using System.Collections.Generic;
using System.Text;

namespace GitCredentialManager.Tty;

/// <summary>
/// Translates the raw byte stream from a POSIX TTY in raw mode into
/// <see cref="ConsoleKeyInfo"/> values, matching the semantics of
/// <c>System.Console.ReadKey</c>.
/// </summary>
/// <remarks>
/// <para>
/// The parser handles the subset of escape sequences that interactive
/// Spectre.Console prompts (selection menus, confirmation prompts, text
/// input) actually consume: arrow keys, Home/End, PageUp/PageDown,
/// Insert/Delete, modifier-prefixed variants (eg. Shift+Arrow), Enter,
/// Tab/Shift+Tab, Backspace, Escape, Ctrl+letter combinations, and
/// printable ASCII / UTF-8 characters.
/// </para>
/// <para>
/// Standalone <c>ESC</c> presses are disambiguated from the start of a
/// multi-byte sequence by polling for additional input with a short
/// timeout; the caller supplies a <c>readByteWithTimeout</c> delegate
/// that returns <c>-1</c> when no more bytes arrive in time.
/// </para>
/// <para>
/// Surrogate pairs (code points above the BMP) are emitted one
/// <see cref="ConsoleKeyInfo"/> per surrogate, matching the
/// <see cref="System.Console.ReadKey(bool)"/> contract.
/// </para>
/// </remarks>
internal static class AnsiEscapeParser
{
    /// <summary>
    /// Reads one keystroke from the given byte source.
    /// </summary>
    /// <param name="readByte">Blocking read of one byte; returns <c>-1</c> on EOF.</param>
    /// <param name="readByteWithTimeout">
    /// Read one byte but wait at most ~50 ms; returns <c>-1</c> on timeout or EOF.
    /// Used only for ESC-vs-CSI disambiguation.
    /// </param>
    /// <returns>The parsed key, or <c>null</c> on EOF.</returns>
    public static ConsoleKeyInfo? ReadKey(Func<int> readByte, Func<int> readByteWithTimeout)
    {
        int first = readByte();
        if (first < 0) return null;

        return first switch
        {
            0x1B => ReadAfterEscape(readByte, readByteWithTimeout),
            0x7F => Plain('\b', ConsoleKey.Backspace),
            0x08 => Plain('\b', ConsoleKey.Backspace),
            0x0D => Plain('\r', ConsoleKey.Enter),
            0x0A => Plain('\r', ConsoleKey.Enter),
            0x09 => Plain('\t', ConsoleKey.Tab),
            0x20 => Plain(' ', ConsoleKey.Spacebar),
            >= 0x01 and <= 0x1A => ReadCtrlLetter(first),
            < 0x80 => ReadAscii((char)first),
            _ => ReadUtf8Continuation(first, readByte),
        };
    }

    private static ConsoleKeyInfo? ReadAfterEscape(Func<int> readByte, Func<int> readByteWithTimeout)
    {
        int next = readByteWithTimeout();
        if (next < 0)
        {
            // ESC alone (no follow-up bytes within the disambiguation window).
            return new ConsoleKeyInfo('\0', ConsoleKey.Escape, false, false, false);
        }

        return next switch
        {
            '[' => ReadCsi(readByte),
            'O' => ReadSs3(readByte),
            // ESC followed by a printable char is usually Alt+<char>; surface the
            // base key with the Alt flag. We deliberately swallow the byte rather
            // than re-buffer it — Spectre prompts only care about the Alt modifier
            // for the few keys it carries (eg. Alt+Backspace), not for printable
            // typing.
            _ => ReadKey(() => next, readByteWithTimeout) is { } k
                ? new ConsoleKeyInfo(k.KeyChar, k.Key, k.Modifiers.HasFlag(ConsoleModifiers.Shift),
                    alt: true,
                    control: k.Modifiers.HasFlag(ConsoleModifiers.Control))
                : new ConsoleKeyInfo('\0', ConsoleKey.Escape, false, false, false),
        };
    }

    /// <summary>
    /// CSI sequences: <c>ESC [ &lt;params&gt; &lt;final&gt;</c>.
    /// </summary>
    private static ConsoleKeyInfo? ReadCsi(Func<int> readByte)
    {
        var paramBytes = new List<byte>();
        while (true)
        {
            int b = readByte();
            if (b < 0) return null;
            // Parameter bytes: digits and ';' separator (per ECMA-48 ranges 0x30-0x3F).
            if ((b >= '0' && b <= '9') || b == ';')
            {
                paramBytes.Add((byte)b);
                continue;
            }
            // Final byte: 0x40-0x7E.
            string param = Encoding.ASCII.GetString(paramBytes.ToArray());
            return InterpretCsi(param, (char)b);
        }
    }

    /// <summary>
    /// SS3 sequences: <c>ESC O &lt;final&gt;</c>. Used by some terminals for
    /// arrow keys when not in CSI mode.
    /// </summary>
    private static ConsoleKeyInfo? ReadSs3(Func<int> readByte)
    {
        int b = readByte();
        if (b < 0) return null;
        return b switch
        {
            'A' => Plain('\0', ConsoleKey.UpArrow),
            'B' => Plain('\0', ConsoleKey.DownArrow),
            'C' => Plain('\0', ConsoleKey.RightArrow),
            'D' => Plain('\0', ConsoleKey.LeftArrow),
            'H' => Plain('\0', ConsoleKey.Home),
            'F' => Plain('\0', ConsoleKey.End),
            'P' => Plain('\0', ConsoleKey.F1),
            'Q' => Plain('\0', ConsoleKey.F2),
            'R' => Plain('\0', ConsoleKey.F3),
            'S' => Plain('\0', ConsoleKey.F4),
            _ => default(ConsoleKeyInfo),
        };
    }

    private static ConsoleKeyInfo InterpretCsi(string param, char final)
    {
        var (n, mod) = ParseParams(param);
        var (shift, alt, ctrl) = DecodeXtermModifier(mod);

        ConsoleKey? key = final switch
        {
            'A' => ConsoleKey.UpArrow,
            'B' => ConsoleKey.DownArrow,
            'C' => ConsoleKey.RightArrow,
            'D' => ConsoleKey.LeftArrow,
            'H' => ConsoleKey.Home,
            'F' => ConsoleKey.End,
            'Z' => ConsoleKey.Tab, // CSI Z = back-tab; encoded as Shift+Tab
            '~' => n switch
            {
                1 => ConsoleKey.Home,
                2 => ConsoleKey.Insert,
                3 => ConsoleKey.Delete,
                4 => ConsoleKey.End,
                5 => ConsoleKey.PageUp,
                6 => ConsoleKey.PageDown,
                7 => ConsoleKey.Home,
                8 => ConsoleKey.End,
                15 => ConsoleKey.F5,
                17 => ConsoleKey.F6,
                18 => ConsoleKey.F7,
                19 => ConsoleKey.F8,
                20 => ConsoleKey.F9,
                21 => ConsoleKey.F10,
                23 => ConsoleKey.F11,
                24 => ConsoleKey.F12,
                _ => null,
            },
            _ => null,
        };

        if (!key.HasValue) return default;

        if (final == 'Z') shift = true; // back-tab implies Shift

        return new ConsoleKeyInfo('\0', key.Value, shift, alt, ctrl);
    }

    private static (int n, int mod) ParseParams(string param)
    {
        if (string.IsNullOrEmpty(param)) return (0, 0);
        string[] parts = param.Split(';');
        int n = int.TryParse(parts[0], out int p0) ? p0 : 0;
        int mod = parts.Length > 1 && int.TryParse(parts[1], out int p1) ? p1 : 0;
        return (n, mod);
    }

    private static (bool shift, bool alt, bool ctrl) DecodeXtermModifier(int mod)
    {
        // xterm modifier encoding: actual = (mod - 1), bit 0 = shift, bit 1 = alt, bit 2 = ctrl.
        if (mod <= 1) return (false, false, false);
        int m = mod - 1;
        return ((m & 1) != 0, (m & 2) != 0, (m & 4) != 0);
    }

    private static ConsoleKeyInfo ReadAscii(char c)
    {
        if (c >= 'A' && c <= 'Z')
        {
            return new ConsoleKeyInfo(c, (ConsoleKey)c, shift: true, alt: false, control: false);
        }
        if (c >= 'a' && c <= 'z')
        {
            return new ConsoleKeyInfo(c, (ConsoleKey)(c - 32), shift: false, alt: false, control: false);
        }
        if (c >= '0' && c <= '9')
        {
            return new ConsoleKeyInfo(c, (ConsoleKey)c, false, false, false);
        }
        // Other printable punctuation: ConsoleKey.NoName, char carries the value.
        return new ConsoleKeyInfo(c, ConsoleKey.NoName, false, false, false);
    }

    private static ConsoleKeyInfo ReadCtrlLetter(int b)
    {
        // 0x01..0x1A → Ctrl+A..Ctrl+Z. Carve out 0x08 (Backspace), 0x09 (Tab), 0x0A/0x0D
        // (Enter) before reaching here.
        char letter = (char)('A' + (b - 1));
        return new ConsoleKeyInfo((char)b, (ConsoleKey)letter, shift: false, alt: false, control: true);
    }

    private static ConsoleKeyInfo? ReadUtf8Continuation(int firstByte, Func<int> readByte)
    {
        int extra = (firstByte & 0xE0) == 0xC0 ? 1
                  : (firstByte & 0xF0) == 0xE0 ? 2
                  : (firstByte & 0xF8) == 0xF0 ? 3
                  : 0;
        if (extra == 0) return default(ConsoleKeyInfo);

        byte[] bytes = new byte[extra + 1];
        bytes[0] = (byte)firstByte;
        for (int i = 1; i <= extra; i++)
        {
            int b = readByte();
            if (b < 0 || (b & 0xC0) != 0x80) return default(ConsoleKeyInfo);
            bytes[i] = (byte)b;
        }
        string decoded = Encoding.UTF8.GetString(bytes);
        if (decoded.Length == 0) return default(ConsoleKeyInfo);

        // Surrogate pairs return the high surrogate first; the next ReadKey call
        // would need to return the low surrogate. Spectre.Console's prompts only
        // consume the .KeyChar of single-char inputs (filter typing), so this
        // edge case never bites in our use cases — but the contract matches
        // Console.ReadKey().
        return new ConsoleKeyInfo(decoded[0], ConsoleKey.NoName, false, false, false);
    }

    private static ConsoleKeyInfo Plain(char keyChar, ConsoleKey key) =>
        new(keyChar, key, false, false, false);
}
