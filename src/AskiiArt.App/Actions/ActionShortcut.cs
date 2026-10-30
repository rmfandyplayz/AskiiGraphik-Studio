// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Input;

namespace AskiiArt.App.Actions;

/// <summary>
/// Modifier keys for a shortcut, written once for every platform.
/// </summary>
[Flags]
internal enum ShortcutModifiers
{
    None = 0,

    /// <summary>Ctrl on Windows and Linux, Cmd on macOS.</summary>
    Command = 1,

    Shift = 2,
    Alt = 4,
}

/// <summary>
/// A platform-neutral keyboard shortcut, such as Command+Q. It becomes a concrete Avalonia
/// <see cref="KeyGesture"/> only when the platform's command key is known (ARCHITECTURE §14.3).
/// </summary>
internal readonly record struct ActionShortcut(Key Key, ShortcutModifiers Modifiers)
{
    public KeyGesture ToKeyGesture(KeyModifiers platformCommandModifiers)
    {
        var modifiers = KeyModifiers.None;

        if (Modifiers.HasFlag(ShortcutModifiers.Command))
            modifiers |= platformCommandModifiers;

        if (Modifiers.HasFlag(ShortcutModifiers.Shift))
            modifiers |= KeyModifiers.Shift;

        if (Modifiers.HasFlag(ShortcutModifiers.Alt))
            modifiers |= KeyModifiers.Alt;

        return new KeyGesture(Key, modifiers);
    }
}
