// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Selection;

/// <summary>The ids of <see cref="SelectionActions"/>. Menus and shortcuts refer to actions by these.</summary>
internal static class SelectionActionIds
{
    public const string SelectAll = "askiiart.select.all";
    public const string Deselect = "askiiart.select.none";
    public const string Invert = "askiiart.select.invert";
    public const string Grow = "askiiart.select.grow";
    public const string Shrink = "askiiart.select.shrink";
    public const string SelectCellsWithGlyphs = "askiiart.select.cellsWithGlyphs";
    public const string SelectCellsWithBackground = "askiiart.select.cellsWithBackground";
}
