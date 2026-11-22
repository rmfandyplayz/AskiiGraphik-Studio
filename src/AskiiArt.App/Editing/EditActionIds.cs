// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Editing;

/// <summary>The ids of <see cref="EditActions"/> and <see cref="ClipboardActions"/>. Menus and shortcuts refer to actions by these.</summary>
internal static class EditActionIds
{
    public const string Undo = "askiiart.edit.undo";
    public const string Redo = "askiiart.edit.redo";
    public const string Cut = "askiiart.edit.cut";
    public const string Copy = "askiiart.edit.copy";
    public const string Paste = "askiiart.edit.paste";
    public const string CopyAsText = "askiiart.edit.copyAsText";
    public const string CopyAsAnsi = "askiiart.edit.copyAsAnsi";
}
