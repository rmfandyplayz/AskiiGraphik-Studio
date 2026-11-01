// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Canvas;
using AskiiArt.Core.Tools;
using Avalonia.Input;

namespace AskiiArt.App.Tests.Canvas;

public sealed class ToolKeymapTests
{
    private static readonly ToolKeymap _windows = ToolKeymap.CreateDefault(isMacOS: false);
    private static readonly ToolKeymap _macOS = ToolKeymap.CreateDefault(isMacOS: true);

    [Theory]
    [InlineData(Key.Left, ToolModifiers.None, ToolKey.Left)]
    [InlineData(Key.Right, ToolModifiers.Command, ToolKey.JumpRight)]
    [InlineData(Key.Up, ToolModifiers.Command | ToolModifiers.Shift, ToolKey.LongJumpUp)]
    [InlineData(Key.Home, ToolModifiers.None, ToolKey.Home)]
    [InlineData(Key.End, ToolModifiers.None, ToolKey.End)]
    [InlineData(Key.Home, ToolModifiers.Command, ToolKey.Top)]
    [InlineData(Key.End, ToolModifiers.Command, ToolKey.Bottom)]
    [InlineData(Key.Back, ToolModifiers.None, ToolKey.Backspace)]
    [InlineData(Key.Enter, ToolModifiers.None, ToolKey.Enter)]
    public void WindowsAndLinux(Key key, ToolModifiers modifiers, ToolKey expected)
    {
        Assert.Equal(expected, _windows.Find(key, modifiers));
    }

    [Theory]
    [InlineData(Key.Left, ToolModifiers.None, ToolKey.Left)]
    [InlineData(Key.Right, ToolModifiers.Alt, ToolKey.JumpRight)]
    [InlineData(Key.Up, ToolModifiers.Command | ToolModifiers.Alt, ToolKey.LongJumpUp)]
    [InlineData(Key.Left, ToolModifiers.Command, ToolKey.Home)]
    [InlineData(Key.Right, ToolModifiers.Command, ToolKey.End)]
    [InlineData(Key.Up, ToolModifiers.Command, ToolKey.Top)]
    [InlineData(Key.Down, ToolModifiers.Command, ToolKey.Bottom)]
    [InlineData(Key.Home, ToolModifiers.None, ToolKey.Home)]
    [InlineData(Key.Back, ToolModifiers.None, ToolKey.Backspace)]
    public void MacOS(Key key, ToolModifiers modifiers, ToolKey expected)
    {
        Assert.Equal(expected, _macOS.Find(key, modifiers));
    }

    [Theory]
    [InlineData(ToolModifiers.Shift)]
    [InlineData(ToolModifiers.Command | ToolModifiers.Alt)]
    public void OtherModifiers_DontMatch_SoShiftStaysFreeForSelection(ToolModifiers modifiers)
    {
        Assert.Null(_windows.Find(Key.Left, modifiers));
    }

    [Fact]
    public void WindowsAndLinux_AltArrow_DoesNothing_BecauseALoneAltOpensTheMenuBar()
    {
        Assert.Null(_windows.Find(Key.Left, ToolModifiers.Alt));
    }

    [Fact]
    public void KeysToolsDontUse_DontMatch()
    {
        Assert.Null(_windows.Find(Key.A, ToolModifiers.None));
        Assert.Null(_macOS.Find(Key.Home, ToolModifiers.Command));
    }
}
