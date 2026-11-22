// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Ramps;

/// <summary>
/// Where a glyph's ink lies in its cell (ARCHITECTURE §7, ADR 0068): the cell cut into <see cref="Columns"/> by
/// <see cref="Rows"/> equal parts, and how much of each part the ink covers, from 0 to 1, as the canvas draws the
/// glyph. The Brush and the Soft Eraser match these against how much of each part a tip covers, so a cell on a shape's
/// edge gets a glyph whose ink is on the shape's side. Immutable.
/// </summary>
public sealed class GlyphShape
{
    /// <summary>How many parts across a cell is cut into.</summary>
    public const int Columns = 4;

    /// <summary>How many parts down a cell is cut into.</summary>
    public const int Rows = 8;

    /// <summary>How many parts a cell is cut into.</summary>
    public const int PartCount = Columns * Rows;

    private readonly double[] _ink;

    /// <summary>
    /// Creates a shape from how much of each part the ink covers, row by row from the top, each row from the left.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// There aren't exactly <see cref="PartCount"/> parts, or one isn't from 0 to 1.
    /// </exception>
    public GlyphShape(ReadOnlySpan<double> ink)
    {
        if (ink.Length != PartCount)
            throw new ArgumentException($"A shape has {PartCount} parts, not {ink.Length}.", nameof(ink));

        foreach (var part in ink)
        {
            if (double.IsNaN(part) || part < 0 || part > 1)
                throw new ArgumentException($"A part's ink must be from 0 to 1, not {part}.", nameof(ink));
        }

        _ink = [.. ink];
        Ink = _ink.Average();
    }

    /// <summary>The shape of a glyph that puts no ink in its cell, as a space.</summary>
    public static GlyphShape Empty { get; } = Even(0);

    /// <summary>How much of each part the ink covers, row by row from the top, each row from the left.</summary>
    public ReadOnlySpan<double> Parts => _ink;

    /// <summary>How much of the whole cell the ink covers, from 0 to 1.</summary>
    public double Ink { get; }

    /// <summary>A shape whose ink covers <paramref name="ink"/> of every part alike.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="ink"/> isn't from 0 to 1.</exception>
    public static GlyphShape Even(double ink)
    {
        if (double.IsNaN(ink) || ink < 0 || ink > 1)
            throw new ArgumentOutOfRangeException(nameof(ink), ink, "Must be from 0 to 1.");

        Span<double> parts = stackalloc double[PartCount];
        parts.Fill(ink);
        return new GlyphShape(parts);
    }

    /// <summary>How much of the part at (<paramref name="column"/>, <paramref name="row"/>) the ink covers.</summary>
    public double InkAt(int column, int row)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(column);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(column, Columns);
        ArgumentOutOfRangeException.ThrowIfNegative(row);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Rows);

        return _ink[row * Columns + column];
    }
}
