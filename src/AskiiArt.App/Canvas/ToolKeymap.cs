// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Tools;
using Avalonia.Input;

namespace AskiiArt.App.Canvas;

/// <summary>
/// Which keys the canvas passes to the active tool, and as which <see cref="ToolKey"/>. Each
/// platform follows its own conventions (ADR 0017): on a Mac, Option+arrow jumps and Cmd+arrow
/// goes to the ends, as in every Mac text field. A key matches only with exactly its modifiers,
/// so Shift+arrow stays free for extending a selection (M4).
/// </summary>
/// <remarks>The keys become rebindable with the shortcut settings (M8).</remarks>
internal sealed class ToolKeymap
{
    private static readonly (Key Key, ToolKey One, ToolKey Jump, ToolKey LongJump)[] _arrows =
    [
        (Key.Left, ToolKey.Left, ToolKey.JumpLeft, ToolKey.LongJumpLeft),
        (Key.Right, ToolKey.Right, ToolKey.JumpRight, ToolKey.LongJumpRight),
        (Key.Up, ToolKey.Up, ToolKey.JumpUp, ToolKey.LongJumpUp),
        (Key.Down, ToolKey.Down, ToolKey.JumpDown, ToolKey.LongJumpDown)
    ];

    private readonly Dictionary<(Key Key, ToolModifiers Modifiers), ToolKey> _toolKeys = [];

    private ToolKeymap()
    {
    }

    /// <summary>The default keys for macOS, or for Windows and Linux.</summary>
    public static ToolKeymap CreateDefault(bool isMacOS)
    {
        var keymap = new ToolKeymap();
        keymap.Bind(Key.Home, ToolModifiers.None, ToolKey.Home);
        keymap.Bind(Key.End, ToolModifiers.None, ToolKey.End);
        keymap.Bind(Key.Back, ToolModifiers.None, ToolKey.Backspace);
        keymap.Bind(Key.Delete, ToolModifiers.None, ToolKey.Delete);
        keymap.Bind(Key.Enter, ToolModifiers.None, ToolKey.Enter);
        keymap.Bind(Key.Escape, ToolModifiers.None, ToolKey.Escape);

        // Command is Ctrl on Windows and Linux and Cmd on macOS; Alt is Option on macOS.
        var jump = isMacOS ? ToolModifiers.Alt : ToolModifiers.Command;
        var longJump = isMacOS ? ToolModifiers.Command | ToolModifiers.Alt : ToolModifiers.Alt;
        foreach (var arrow in _arrows)
        {
            keymap.Bind(arrow.Key, ToolModifiers.None, arrow.One);
            keymap.Bind(arrow.Key, jump, arrow.Jump);
            keymap.Bind(arrow.Key, longJump, arrow.LongJump);
        }

        if (isMacOS)
        {
            keymap.Bind(Key.Left, ToolModifiers.Command, ToolKey.Home);
            keymap.Bind(Key.Right, ToolModifiers.Command, ToolKey.End);
            keymap.Bind(Key.Up, ToolModifiers.Command, ToolKey.Top);
            keymap.Bind(Key.Down, ToolModifiers.Command, ToolKey.Bottom);
        }
        else
        {
            keymap.Bind(Key.Home, ToolModifiers.Command, ToolKey.Top);
            keymap.Bind(Key.End, ToolModifiers.Command, ToolKey.Bottom);
        }

        return keymap;
    }

    /// <summary>The default keys for the platform the app is running on.</summary>
    public static ToolKeymap ForCurrentPlatform() => CreateDefault(OperatingSystem.IsMacOS());

    /// <summary>The tool key for <paramref name="key"/> pressed with exactly <paramref name="modifiers"/>, or null.</summary>
    public ToolKey? Find(Key key, ToolModifiers modifiers)
    {
        if (_toolKeys.TryGetValue((key, modifiers), out var toolKey))
            return toolKey;

        return null;
    }

    private void Bind(Key key, ToolModifiers modifiers, ToolKey toolKey) => _toolKeys.Add((key, modifiers), toolKey);
}
