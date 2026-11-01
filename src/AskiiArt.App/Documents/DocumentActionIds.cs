// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Documents;

/// <summary>The ids of <see cref="DocumentActions"/>. Menus, the toolbar and shortcuts refer to actions by these.</summary>
internal static class DocumentActionIds
{
    public const string New = "askiiart.document.new";
    public const string Open = "askiiart.document.open";
    public const string Save = "askiiart.document.save";
    public const string SaveAs = "askiiart.document.saveAs";
    public const string ExportAsText = "askiiart.document.exportAsText";
    public const string ResizeCanvas = "askiiart.document.resizeCanvas";
    public const string Font = "askiiart.document.font";
    public const string AsciiOnly = "askiiart.document.asciiOnly";

    /// <summary>
    /// Debug builds only: replaces the document with the busy 500 × 500 sample, for performance checks.
    /// </summary>
    public const string OpenSampleDocument = "askiiart.debug.openSampleDocument";
}
