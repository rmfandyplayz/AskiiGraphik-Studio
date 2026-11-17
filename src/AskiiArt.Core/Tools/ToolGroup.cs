// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Tools;

/// <summary>
/// Tools that share one button on the Tools toolbar and one letter key (ADR 0049). The button and the letter choose
/// the group's tool that was used last. While one of its tools is active, the number keys 1 to 9 choose its tools in
/// order. Every tool is in exactly one group.
/// </summary>
/// <param name="Id">A stable id, for example "askiiart.group.brush". Never shown to the user.</param>
/// <param name="NameKey">The App's resource key for the group's localized name, shown on its button.</param>
/// <param name="IconKey">The group's icon key in the icon lookup (ARCHITECTURE §14.5).</param>
/// <param name="ShortcutLetter">The capital letter that picks the group, if any (ADR 0033).</param>
/// <param name="ToolIds">
/// The group's tools, in the order the number keys choose them. The first is the one chosen until another is.
/// </param>
public sealed record ToolGroup(
    string Id,
    string NameKey,
    string IconKey,
    char? ShortcutLetter,
    IReadOnlyList<string> ToolIds);
