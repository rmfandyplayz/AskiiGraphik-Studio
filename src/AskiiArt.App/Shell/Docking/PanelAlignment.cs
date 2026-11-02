// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Data.Converters;
using Avalonia.Layout;

namespace AskiiArt.App.Shell.Docking;

/// <summary>
/// How a panel's widget sits in its dock. The toolbar hugs its content at the right of its row, so
/// art shows in the rest of the row (ADR 0029); other panels fill their dock.
/// </summary>
internal static class PanelAlignment
{
    public static readonly IValueConverter ForPanelId = new FuncValueConverter<string?, HorizontalAlignment>(
        panelId => panelId == ShellDockIds.ToolbarPanel ? HorizontalAlignment.Right : HorizontalAlignment.Stretch);
}
