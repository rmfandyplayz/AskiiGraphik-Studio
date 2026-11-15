// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Editing;

namespace AskiiArt.Core.Tools;

/// <summary>
/// What a new tool works with: the document's editing session, and the colours shared by every document.
/// </summary>
/// <param name="Session">The editing session the tool changes.</param>
/// <param name="Colors">The current FG and BG colours, the same for every open document (ADR 0042).</param>
public sealed record ToolContext(EditingSession Session, PaintColors Colors);
