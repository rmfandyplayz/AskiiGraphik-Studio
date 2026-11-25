// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Tools.Lines;

/// <summary>
/// How a line or a shape's outline chooses the character for each cell it passes through (ADR 0078, ADR 0079).
/// </summary>
public enum LineStyle
{
    /// <summary>
    /// Characters by the way the line runs where it crosses each cell: <c>-</c>, <c>|</c>, <c>/</c> and <c>\</c>, with
    /// <c>+</c> where it turns a corner.
    /// </summary>
    Standard,

    /// <summary>
    /// The same characters as <see cref="Standard"/>, by the step from the cell before to the cell after instead, so a
    /// shallow line shows each step it takes as a <c>/</c> or <c>\</c> between runs of <c>-</c>.
    /// </summary>
    StandardByStep,

    /// <summary>
    /// The character whose measured ink best follows the line through each cell, such as <c>_</c>, <c>.</c>,
    /// <c>-</c> and <c>'</c> along a shallow slope.
    /// </summary>
    Precise,

    /// <summary>One chosen character in every cell.</summary>
    SingleCharacter
}
