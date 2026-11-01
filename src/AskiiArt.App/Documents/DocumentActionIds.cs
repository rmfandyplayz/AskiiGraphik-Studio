// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Documents;

/// <summary>The ids of <see cref="DocumentActions"/>. Menus, the toolbar and shortcuts refer to actions by these.</summary>
internal static class DocumentActionIds
{
    public const string New = "askiiart.document.new";
    public const string ResizeCanvas = "askiiart.document.resizeCanvas";
    public const string Font = "askiiart.document.font";
    public const string AsciiOnly = "askiiart.document.asciiOnly";
}
