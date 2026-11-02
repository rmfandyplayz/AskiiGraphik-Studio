// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Data.Converters;
using Avalonia.Layout;

namespace AskiiArt.App.Shell.Docking;

/// <summary>
/// Where a widget sits in its dock, from the dock's id. The fixed rows hug their content, so the art
/// shows beside them; other panels fill their dock. Public, because AXAML's {x:Static} needs it.
/// </summary>
public static class WidgetConverters
{
    public static readonly IValueConverter HorizontalAlignmentOf =
        new FuncValueConverter<string?, HorizontalAlignment>(dockId => dockId switch
        {
            ShellDockIds.MenuBarDock or ShellDockIds.StatusBarDock => HorizontalAlignment.Left,
            ShellDockIds.ToolbarDock => HorizontalAlignment.Center,
            _ => HorizontalAlignment.Stretch,
        });
}
