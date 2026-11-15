// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Tools;

/// <summary>
/// One of the two current colours (ADR 0042). <see cref="PaintColors.Target"/> says which one is chosen.
/// </summary>
public enum PaintColorTarget
{
    /// <summary>The FG colour, for text.</summary>
    Foreground,

    /// <summary>The BG colour, for backgrounds.</summary>
    Background
}
