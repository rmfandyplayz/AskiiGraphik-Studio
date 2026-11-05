// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Shell.Docking;

/// <summary>Debug builds only: the actions that put a sample sub-toolbar on each toolbar.</summary>
internal static class SubToolbarSampleActionIds
{
    public const string OnFile = "askiiart.debug.subToolbar.onFile";
    public const string OnTools = "askiiart.debug.subToolbar.onTools";
    public const string OnEdit = "askiiart.debug.subToolbar.onEdit";
    public const string OnView = "askiiart.debug.subToolbar.onView";

    /// <summary>Whether the sample is long enough to reach its length limits, and scroll.</summary>
    public const string Long = "askiiart.debug.subToolbar.long";
}
