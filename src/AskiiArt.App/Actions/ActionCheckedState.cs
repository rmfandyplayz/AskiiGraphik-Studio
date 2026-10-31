// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Actions;

/// <summary>
/// The on/off state of a toggle action, such as Show Grid. Menus show it as a check mark and
/// update it when <see cref="Changed"/> is raised.
/// </summary>
internal sealed class ActionCheckedState(Func<bool> isChecked)
{
    public event EventHandler? Changed;

    public bool IsChecked => isChecked();

    /// <summary>Call after the underlying state changes, so menus refresh their check mark.</summary>
    public void NotifyChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
