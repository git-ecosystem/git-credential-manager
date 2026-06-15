using System;
using GitCredentialManager.Interop.Windows;
using GitCredentialManager.Interop.Windows.Native;
using Xunit;

namespace GitCredentialManager.Tests.Interop.Windows;

public class WindowsAnsiConsoleInputTests
{
    [Fact]
    public void ToConsoleKeyInfo_PlainLetter_HasNoModifiers()
    {
        var ev = new KEY_EVENT_RECORD
        {
            bKeyDown = true,
            wVirtualKeyCode = (ushort)ConsoleKey.A,
            UnicodeChar = 'a',
            dwControlKeyState = 0,
        };

        ConsoleKeyInfo info = WindowsAnsiConsoleInput.ToConsoleKeyInfo(ev);

        Assert.Equal(ConsoleKey.A, info.Key);
        Assert.Equal('a', info.KeyChar);
        Assert.Equal((ConsoleModifiers)0, info.Modifiers);
    }

    [Fact]
    public void ToConsoleKeyInfo_ShiftedLetter_SetsShift()
    {
        var ev = new KEY_EVENT_RECORD
        {
            bKeyDown = true,
            wVirtualKeyCode = (ushort)ConsoleKey.A,
            UnicodeChar = 'A',
            dwControlKeyState = ControlKeyState.ShiftPressed,
        };

        ConsoleKeyInfo info = WindowsAnsiConsoleInput.ToConsoleKeyInfo(ev);

        Assert.True(info.Modifiers.HasFlag(ConsoleModifiers.Shift));
        Assert.False(info.Modifiers.HasFlag(ConsoleModifiers.Control));
        Assert.False(info.Modifiers.HasFlag(ConsoleModifiers.Alt));
    }

    [Theory]
    [InlineData(ControlKeyState.LeftCtrlPressed)]
    [InlineData(ControlKeyState.RightCtrlPressed)]
    public void ToConsoleKeyInfo_CtrlVariants_SetControl(ControlKeyState state)
    {
        var ev = new KEY_EVENT_RECORD
        {
            bKeyDown = true,
            wVirtualKeyCode = (ushort)ConsoleKey.C,
            UnicodeChar = '\u0003',
            dwControlKeyState = state,
        };

        ConsoleKeyInfo info = WindowsAnsiConsoleInput.ToConsoleKeyInfo(ev);

        Assert.True(info.Modifiers.HasFlag(ConsoleModifiers.Control));
    }

    [Theory]
    [InlineData(ControlKeyState.LeftAltPressed)]
    [InlineData(ControlKeyState.RightAltPressed)]
    public void ToConsoleKeyInfo_AltVariants_SetAlt(ControlKeyState state)
    {
        var ev = new KEY_EVENT_RECORD
        {
            bKeyDown = true,
            wVirtualKeyCode = (ushort)ConsoleKey.A,
            UnicodeChar = '\0',
            dwControlKeyState = state,
        };

        ConsoleKeyInfo info = WindowsAnsiConsoleInput.ToConsoleKeyInfo(ev);

        Assert.True(info.Modifiers.HasFlag(ConsoleModifiers.Alt));
    }

    [Fact]
    public void ToConsoleKeyInfo_CombinedModifiers_AllSet()
    {
        var ev = new KEY_EVENT_RECORD
        {
            bKeyDown = true,
            wVirtualKeyCode = (ushort)ConsoleKey.UpArrow,
            UnicodeChar = '\0',
            dwControlKeyState = ControlKeyState.ShiftPressed
                              | ControlKeyState.LeftCtrlPressed
                              | ControlKeyState.LeftAltPressed,
        };

        ConsoleKeyInfo info = WindowsAnsiConsoleInput.ToConsoleKeyInfo(ev);

        Assert.True(info.Modifiers.HasFlag(ConsoleModifiers.Shift));
        Assert.True(info.Modifiers.HasFlag(ConsoleModifiers.Control));
        Assert.True(info.Modifiers.HasFlag(ConsoleModifiers.Alt));
    }

    [Fact]
    public void ToConsoleKeyInfo_PassesThroughNamedKeys()
    {
        var ev = new KEY_EVENT_RECORD
        {
            bKeyDown = true,
            wVirtualKeyCode = (ushort)ConsoleKey.UpArrow,
            UnicodeChar = '\0',
            dwControlKeyState = 0,
        };

        ConsoleKeyInfo info = WindowsAnsiConsoleInput.ToConsoleKeyInfo(ev);

        Assert.Equal(ConsoleKey.UpArrow, info.Key);
    }

    [Fact]
    public void ToConsoleKeyInfo_IgnoresLockToggles_OnlyHonoursActiveModifiers()
    {
        // Caps/Num/Scroll lock are state-toggles, not modifiers; we should not
        // surface them through ConsoleModifiers.
        var ev = new KEY_EVENT_RECORD
        {
            bKeyDown = true,
            wVirtualKeyCode = (ushort)ConsoleKey.A,
            UnicodeChar = 'A',
            dwControlKeyState = ControlKeyState.CapslockOn | ControlKeyState.NumlockOn,
        };

        ConsoleKeyInfo info = WindowsAnsiConsoleInput.ToConsoleKeyInfo(ev);

        Assert.Equal((ConsoleModifiers)0, info.Modifiers);
    }
}
