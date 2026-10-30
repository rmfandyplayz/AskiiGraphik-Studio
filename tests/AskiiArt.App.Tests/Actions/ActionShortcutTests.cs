// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using Avalonia.Input;

namespace AskiiArt.App.Tests.Actions;

public sealed class ActionShortcutTests
{
    [Fact]
    public void CommandModifier_BecomesCtrl_OnWindowsAndLinux()
    {
        var shortcut = new ActionShortcut(Key.Q, ShortcutModifiers.Command);

        Assert.Equal(new KeyGesture(Key.Q, KeyModifiers.Control), shortcut.ToKeyGesture(KeyModifiers.Control));
    }

    [Fact]
    public void CommandModifier_BecomesCmd_OnMacOS()
    {
        var shortcut = new ActionShortcut(Key.Q, ShortcutModifiers.Command);

        Assert.Equal(new KeyGesture(Key.Q, KeyModifiers.Meta), shortcut.ToKeyGesture(KeyModifiers.Meta));
    }

    [Fact]
    public void ShiftAndAlt_AreKeptAsTheyAre()
    {
        var shortcut = new ActionShortcut(Key.Z, ShortcutModifiers.Command | ShortcutModifiers.Shift | ShortcutModifiers.Alt);

        Assert.Equal(
            new KeyGesture(Key.Z, KeyModifiers.Meta | KeyModifiers.Shift | KeyModifiers.Alt),
            shortcut.ToKeyGesture(KeyModifiers.Meta));
    }

    [Fact]
    public void NoModifiers_GivesAPlainKey()
    {
        var shortcut = new ActionShortcut(Key.F1, ShortcutModifiers.None);

        Assert.Equal(new KeyGesture(Key.F1), shortcut.ToKeyGesture(KeyModifiers.Control));
    }
}
