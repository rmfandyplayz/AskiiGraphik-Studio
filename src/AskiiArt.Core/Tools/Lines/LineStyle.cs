// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Tools.Lines;

/// <summary>
/// How a line or a shape's outline chooses the character for each cell it passes through (ADR 0078, ADR 0079,
/// ADR 0081).
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

    /// <summary>
    /// Light box-drawing characters by the way the line runs, as <see cref="Standard"/> chooses them: <c>─</c>,
    /// <c>│</c>, <c>╱</c> and <c>╲</c>, with corners such as <c>┌</c>.
    /// </summary>
    Box,

    /// <summary>Heavy box-drawing characters, as <see cref="Box"/>: <c>━</c> and <c>┃</c>, with corners such as <c>┏</c>.</summary>
    HeavyBox,

    /// <summary>Double box-drawing characters, as <see cref="Box"/>: <c>═</c> and <c>║</c>, with corners such as <c>╔</c>.</summary>
    DoubleBox,

    /// <summary>One chosen character in every cell.</summary>
    SingleCharacter
}

/// <summary>What sort of characters each <see cref="LineStyle"/> draws.</summary>
public static class LineStyles
{
    /// <summary>Whether <paramref name="style"/> draws box-drawing characters, which need a Unicode document.</summary>
    public static bool IsBox(this LineStyle style) =>
        style is LineStyle.Box or LineStyle.HeavyBox or LineStyle.DoubleBox;
}
