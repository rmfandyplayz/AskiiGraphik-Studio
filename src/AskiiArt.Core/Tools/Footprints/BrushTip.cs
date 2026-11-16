// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Tools.Footprints;

/// <summary>The shape of a brush tip.</summary>
public enum BrushShape
{
    /// <summary>A rectangle of cells.</summary>
    Square = 0,

    /// <summary>The cells whose centres lie inside a circle or ellipse.</summary>
    Circle = 1
}

/// <summary>How a brush tip's height follows from its size (ADR 0047).</summary>
public enum BrushProportions
{
    /// <summary>
    /// The tip looks square or round on screen. Cells are taller than they're wide, so the tip is fewer cells tall
    /// than wide: about half as many in the bundled fonts.
    /// </summary>
    Perceptual = 0,

    /// <summary>The tip counts cells: a size of 10 is 10 cells wide and 10 cells tall.</summary>
    Mathematical = 1
}

/// <summary>
/// What a sized tool paints with (ARCHITECTURE §8.1, ADR 0047): a size in cells, a shape, and whether the shape is
/// measured on screen or in cells.
/// </summary>
public sealed record BrushTip
{
    /// <summary>The smallest size: one cell.</summary>
    public const int MinimumSize = 1;

    /// <summary>The largest size, in cells.</summary>
    public const int MaximumSize = 100;

    /// <summary>Creates a tip.</summary>
    /// <param name="size">The width in cells, from <see cref="MinimumSize"/> to <see cref="MaximumSize"/>.</param>
    /// <param name="shape">The shape.</param>
    /// <param name="proportions">Whether the shape is measured on screen or in cells.</param>
    public BrushTip(int size, BrushShape shape, BrushProportions proportions)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(size, MinimumSize);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(size, MaximumSize);
        if (!Enum.IsDefined(shape))
            throw new ArgumentOutOfRangeException(nameof(shape), shape, "Unknown brush shape.");

        if (!Enum.IsDefined(proportions))
            throw new ArgumentOutOfRangeException(nameof(proportions), proportions, "Unknown brush proportions.");

        Size = size;
        Shape = shape;
        Proportions = proportions;
    }

    /// <summary>The width in cells.</summary>
    public int Size { get; }

    /// <summary>The shape.</summary>
    public BrushShape Shape { get; }

    /// <summary>Whether the shape is measured on screen or in cells.</summary>
    public BrushProportions Proportions { get; }
}
