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
/// <see cref="LineStyle"/>s (ADR 0078, ADR 0081). Made for one document's font, cell shape and character set.
/// </summary>
public sealed class LineGlyphPicker
{
    private static readonly LineCharacters _ascii = new('-', '|', '\\', '/', '+', LineWeight.Light);
    private static readonly LineCharacters _box = new('─', '│', '╲', '╱', '┼', LineWeight.Light);
    private static readonly LineCharacters _heavyBox = new('━', '┃', '╲', '╱', '╋', LineWeight.Heavy);
    private static readonly LineCharacters _doubleBox = new('═', '║', '╲', '╱', '╬', LineWeight.Double);

    private readonly double _cellWidthToHeight;
    private readonly bool _canHoldBoxCharacters;
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
        _canHoldBoxCharacters = LineStyleOptions.CanHoldBoxCharacters(characterSet);
        _preciseMatcher = PreciseLineMatcher.Create(cellWidthToHeight, glyphs, characterSet);
    }

    /// <summary>
    /// The style lines are drawn in when <paramref name="style"/> is chosen: Standard for a box-drawing style in a
    /// document that can't hold box-drawing characters, otherwise the style itself.
    /// </summary>
    public LineStyle Drawn(LineStyle style) =>
        style.IsBox() && !_canHoldBoxCharacters ? LineStyle.Standard : style;

    /// <summary>
    /// The cells <paramref name="path"/> passes through, in the order it goes, each with its character in
    /// <paramref name="style"/>, or in the style it's <see cref="Drawn"/> in. <paramref name="singleCharacter"/> is the
    /// character for <see cref="LineStyle.SingleCharacter"/>. Precise falls back to Standard where the character set
    /// allows none of its characters.
    /// </summary>
    public IReadOnlyList<LineGlyph> Pick(LinePath path, LineStyle style, Rune singleCharacter)
    {
        ArgumentNullException.ThrowIfNull(path);
        style = Drawn(style);
        var characters = CharactersOf(style);
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
                    precise.GlyphFor(cell.Cell, cell.Offset) ?? ByDirection(cells, index, path.IsClosed, _ascii),
                _ => ByDirection(cells, index, path.IsClosed, characters)
            };
            glyphs[index] = new LineGlyph(cell.Cell, glyph);
        }

        return glyphs;
    }

    private static LineCharacters CharactersOf(LineStyle style) => style switch
    {
        LineStyle.Box => _box,
        LineStyle.HeavyBox => _heavyBox,
        LineStyle.DoubleBox => _doubleBox,
        _ => _ascii
    };

    private Rune ByDirection(List<LinePathCell> cells, int index, bool closed, LineCharacters characters)
    {
        var cell = cells[index];
        if (cell.IsCorner)
            return CornerOf(cells, index, closed, characters);

        return characters.For(DirectionOf(cell.DirectionX, cell.DirectionY));
    }

    // A box-drawing corner joins the cells on either side of it, as ┌ joins the cells to its right and below, where
    // both are beside it rather than across a corner. Any other corner is a cross, as + is in Standard.
    private static Rune CornerOf(List<LinePathCell> cells, int index, bool closed, LineCharacters characters)
    {
        if (characters == _ascii || cells.Count < 3 || (!closed && (index == 0 || index == cells.Count - 1)))
            return characters.Cross;

        var cell = cells[index].Cell;
        var toBefore = ArmToward(cell, cells[(index - 1 + cells.Count) % cells.Count].Cell, characters.Weight);
        var toAfter = ArmToward(cell, cells[(index + 1) % cells.Count].Cell, characters.Weight);
        var arms = toBefore.Over(toAfter);
        if (arms == toBefore || arms == toAfter)
            return characters.Cross;

        return LineJoins.BoxCharacterFor(arms) ?? characters.Cross;
    }

    // An arm of `weight` from `cell` toward `neighbour` when it's beside the cell, otherwise none.
    private static LineArms ArmToward(CellPoint cell, CellPoint neighbour, LineWeight weight)
    {
        var none = LineWeight.None;
        return (neighbour.X - cell.X, neighbour.Y - cell.Y) switch
        {
            (0, -1) => new LineArms(weight, none, none, none),
            (1, 0) => new LineArms(none, weight, none, none),
            (0, 1) => new LineArms(none, none, weight, none),
            (-1, 0) => new LineArms(none, none, none, weight),
            _ => default
        };
    }

    // The way from the cell before to the cell after; at an open path's ends, from or to the cell itself.
    private Rune ByStep(List<LinePathCell> cells, int index, bool closed)
    {
        var cell = cells[index];
        if (cell.IsCorner || cells.Count == 1)
            return ByDirection(cells, index, closed, _ascii);

        var before = cell.Cell;
        if (index > 0 || closed)
            before = cells[(index - 1 + cells.Count) % cells.Count].Cell;

        var after = cell.Cell;
        if (index < cells.Count - 1 || closed)
            after = cells[(index + 1) % cells.Count].Cell;
        return _ascii.For(DirectionOf(after.X - before.X, after.Y - before.Y));
    }

    // Each character stands for the angle it's drawn at on screen: - flat, | upright, and \ and / corner to corner of
    // their cell. A direction gets the character whose angle is nearest.
    private LineDirection DirectionOf(double across, double down)
    {
        if (across == 0 && down == 0)
            return LineDirection.Flat;

        var degrees = Math.Atan2(down, across * _cellWidthToHeight) * 180 / Math.PI;
        if (degrees < 0)
            degrees += 180;

        if (degrees >= 180)
            degrees -= 180;

        var diagonal = Math.Atan2(1, _cellWidthToHeight) * 180 / Math.PI;
        var flatToDiagonal = diagonal / 2;
        var diagonalToUpright = (diagonal + 90) / 2;
        if (degrees < flatToDiagonal || degrees >= 180 - flatToDiagonal)
            return LineDirection.Flat;

        if (degrees < diagonalToUpright)
            return LineDirection.Falling;

        return degrees <= 180 - diagonalToUpright ? LineDirection.Upright : LineDirection.Rising;
    }

    // The ways a line can look like it runs through a cell.
    private enum LineDirection
    {
        Flat,
        Upright,
        Falling,
        Rising
    }

    // One style's characters for each way a line runs and for a corner that isn't a box-drawing one, and the weight
    // of its box-drawing corners.
    private sealed record LineCharacters(
        char Flat,
        char Upright,
        char Falling,
        char Rising,
        char CrossCharacter,
        LineWeight Weight)
    {
        public Rune Cross => new(CrossCharacter);

        public Rune For(LineDirection direction) => new(direction switch
        {
            LineDirection.Upright => Upright,
            LineDirection.Falling => Falling,
            LineDirection.Rising => Rising,
            _ => Flat
        });
    }
}
