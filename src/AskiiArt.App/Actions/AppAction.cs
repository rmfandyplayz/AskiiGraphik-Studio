// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Windows.Input;

namespace AskiiArt.App.Actions;

/// <summary>
/// An operation the user can invoke (ARCHITECTURE §14.2). Menus, toolbars and shortcuts refer to
/// it by <see cref="Id"/> and never call its logic directly, so they can be rearranged or rebound
/// without touching that logic.
/// </summary>
/// <remarks>
/// Whether the action is enabled comes from <see cref="Command"/>'s CanExecute. A checked state
/// for toggle actions arrives with the first toggle action (the grid toggle, step 1.4).
/// </remarks>
internal sealed class AppAction
{
    public AppAction(string id, string labelKey, ICommand command, IReadOnlyList<ActionShortcut>? defaultShortcuts = null, string? iconKey = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(labelKey);
        ArgumentNullException.ThrowIfNull(command);

        Id = id;
        LabelKey = labelKey;
        Command = command;
        DefaultShortcuts = defaultShortcuts ?? [];
        IconKey = iconKey;
    }

    /// <summary>A stable id, for example "askiiart.quit". Never shown to the user.</summary>
    public string Id { get; }

    /// <summary>The key of the label in Strings.resx. Use <c>nameof(Strings.X)</c>.</summary>
    public string LabelKey { get; }

    public ICommand Command { get; }

    public IReadOnlyList<ActionShortcut> DefaultShortcuts { get; }

    /// <summary>The icon's key in the icon lookup (ARCHITECTURE §14.5), or null for no icon.</summary>
    public string? IconKey { get; }
}
