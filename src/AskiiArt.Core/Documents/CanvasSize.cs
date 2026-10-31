// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Documents;

/// <summary>
/// The size of a canvas in cells (ARCHITECTURE §5.7). Both sides are between
/// <see cref="MinimumSide"/> and <see cref="MaximumSide"/>. The limits are explained in ADR 0011.
/// </summary>
/// <remarks>
/// <c>default(CanvasSize)</c> is 0 × 0 and isn't a valid size; always use the constructor.
/// </remarks>
public readonly record struct CanvasSize
{
    /// <summary>The smallest allowed width or height.</summary>
    public const int MinimumSide = 1;

    /// <summary>The largest allowed width or height (the hard limit). It guards against typos.</summary>
    public const int MaximumSide = 4096;

    /// <summary>
    /// The largest width or height that doesn't need a warning (the soft limit). Above it the
    /// canvas is allowed, but may use a lot of memory and feel slow.
    /// </summary>
    public const int RecommendedMaximumSide = 500;

    /// <summary>Creates a canvas size.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A side is outside the allowed range.</exception>
    public CanvasSize(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, MinimumSide);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(width, MaximumSide);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, MinimumSide);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(height, MaximumSide);

        Width = width;
        Height = height;
    }

    /// <summary>The number of columns.</summary>
    public int Width { get; }

    /// <summary>The number of rows.</summary>
    public int Height { get; }

    /// <summary>The total number of cells.</summary>
    public int CellCount => Width * Height;

    /// <summary>The rect that covers the whole canvas, with its top left at (0, 0).</summary>
    public CellRect Bounds => new(0, 0, Width, Height);

    /// <summary>True when either side is above <see cref="RecommendedMaximumSide"/>, so the user should be warned.</summary>
    public bool ExceedsRecommendedSize => Width > RecommendedMaximumSide || Height > RecommendedMaximumSide;

    /// <summary>True when both sides are within the allowed range. Lets input be checked without catching exceptions.</summary>
    public static bool IsValid(int width, int height) =>
        width is >= MinimumSide and <= MaximumSide && height is >= MinimumSide and <= MaximumSide;

    /// <summary>True when <paramref name="point"/> is a cell of this canvas.</summary>
    public bool Contains(CellPoint point) => Bounds.Contains(point);

    /// <inheritdoc/>
    public override string ToString() => $"{Width} × {Height}";
}
