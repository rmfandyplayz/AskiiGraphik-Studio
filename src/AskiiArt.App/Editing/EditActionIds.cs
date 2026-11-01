// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Editing;

/// <summary>The ids of <see cref="EditActions"/>. Menus and shortcuts refer to actions by these.</summary>
internal static class EditActionIds
{
    public const string Undo = "askiiart.edit.undo";
    public const string Redo = "askiiart.edit.redo";
    public const string CopyAsText = "askiiart.edit.copyAsText";
}
