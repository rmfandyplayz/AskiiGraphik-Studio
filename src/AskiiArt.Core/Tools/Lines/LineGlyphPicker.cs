// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Ramps;

namespace AskiiArt.Core.Tools.Lines;

/// <summary>One cell of a drawn line and the character it gets.</summary>
public readonly record struct LineGlyph(CellPoint Cell, Rune Glyph);

/// <summary>
/// Chooses the characters a line or a shape's outline puts in the cells it passes through, in one of the
/// <see cref="LineStyle"/>s (step 5.1). Made for one document's font, cell shape and character set.
/// </summary>
public sealed class LineGlyphPicker
{
    private static readonly Rune _horizontal = new('-');
    private static readonly Rune _vertical = new('|');
    private static readonly Rune _fallingDiagonal = new('\\');
    private static readonly Rune _risingDiagonal = new('/');
    private static readonly Rune _corner = new('+');

    private readonly double _cellWidthToHeight;
    private readonly PreciseLineMatcher? _preciseMatcher;

    /// <summary>
    /// Creates a picker. <paramref name="cellWidthToHeight"/> is how wide a cell looks next to its height, which
    /// decides how steep a line looks; <paramref name="glyphs"/> measures where each character's ink lies, for
    /// <see cref="LineStyle.Precise"/>; and only characters <paramref name="characterSet"/> allows are chosen.
    /// </summary>
    public LineGlyphPicker(double cellWidthToHeight, IGlyphDensityProvider glyphs, CharacterSet characterSet)
    {
        ArgumentNullException.ThrowIfNull(glyphs);
        if (!(cellWidthToHeight > 0))
            throw new ArgumentOutOfRangeException(nameof(cellWidthToHeight), cellWidthToHeight, "Must be above 0.");

        _cellWidthToHeight = cellWidthToHeight;
        _preciseMatcher = PreciseLineMatcher.Create(cellWidthToHeight, glyphs, characterSet);
    }

    /// <summary>
    /// The cells <paramref name="path"/> passes through, in the order it goes, each with its character in
    /// <paramref name="style"/>. <paramref name="singleCharacter"/> is the character for
    /// <see cref="LineStyle.SingleCharacter"/>. Precise falls back to Standard where the character set allows none of
    /// its characters.
    /// </summary>
    public IReadOnlyList<LineGlyph> Pick(LinePath path, LineStyle style, Rune singleCharacter)
    {
        ArgumentNullException.ThrowIfNull(path);
        var cells = LinePathCells.Of(path, _cellWidthToHeight);
        var glyphs = new LineGlyph[cells.Count];
        var precise = style == LineStyle.Precise ? _preciseMatcher?.Prepare(path) : null;
        for (var index = 0; index < cells.Count; index++)
        {
            var cell = cells[index];
            var glyph = style switch
            {
                LineStyle.SingleCharacter => singleCharacter,
                LineStyle.StandardByStep => ByStep(cells, index, path.IsClosed),
                LineStyle.Precise when precise is not null && !cell.IsCorner =>
                    precise.GlyphFor(cell.Cell, cell.Offset) ?? ByDirection(cell),
                _ => ByDirection(cell)
            };
            glyphs[index] = new LineGlyph(cell.Cell, glyph);
        }

        return glyphs;
    }

    private Rune ByDirection(LinePathCell cell) =>
        cell.IsCorner ? _corner : ForDirection(cell.DirectionX, cell.DirectionY);

    // The way from the cell before to the cell after; at an open path's ends, from or to the cell itself.
    private Rune ByStep(List<LinePathCell> cells, int index, bool closed)
    {
        var cell = cells[index];
        if (cell.IsCorner || cells.Count == 1)
            return ByDirection(cell);

        var before = cell.Cell;
        if (index > 0 || closed)
            before = cells[(index - 1 + cells.Count) % cells.Count].Cell;

        var after = cell.Cell;
        if (index < cells.Count - 1 || closed)
            after = cells[(index + 1) % cells.Count].Cell;
        return ForDirection(after.X - before.X, after.Y - before.Y);
    }

    // Each character stands for the angle it's drawn at on screen: - flat, | upright, and \ and / corner to corner of
    // their cell. A direction gets the character whose angle is nearest.
    private Rune ForDirection(double across, double down)
    {
        if (across == 0 && down == 0)
            return _horizontal;

        var degrees = Math.Atan2(down, across * _cellWidthToHeight) * 180 / Math.PI;
        if (degrees < 0)
            degrees += 180;

        if (degrees >= 180)
            degrees -= 180;

        var diagonal = Math.Atan2(1, _cellWidthToHeight) * 180 / Math.PI;
        var flatToDiagonal = diagonal / 2;
        var diagonalToUpright = (diagonal + 90) / 2;
        if (degrees < flatToDiagonal || degrees >= 180 - flatToDiagonal)
            return _horizontal;

        if (degrees < diagonalToUpright)
            return _fallingDiagonal;

        return degrees <= 180 - diagonalToUpright ? _vertical : _risingDiagonal;
    }
}
