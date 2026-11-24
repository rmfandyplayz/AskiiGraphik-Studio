// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Tools.Lines;

/// <summary>How a line or a shape's outline chooses the character for each cell it passes through (step 5.1).</summary>
public enum LineStyle
{
    /// <summary>
    /// Characters by the way the line runs where it crosses each cell: <c>-</c>, <c>|</c>, <c>/</c> and <c>\</c>, with
    /// <c>+</c> where it turns a corner.
    /// </summary>
    Standard,

    /// <summary>
    /// Step 5.1's second candidate for <see cref="Standard"/>: characters by the step from the cell before to the cell
    /// after, so a shallow line shows each step it takes. Temporary: step 5.2 keeps one of the two.
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
