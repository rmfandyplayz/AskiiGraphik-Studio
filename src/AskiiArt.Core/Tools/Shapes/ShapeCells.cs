// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Tools.Lines;

namespace AskiiArt.Core.Tools.Shapes;

/// <summary>
/// The cells one drawing of a shape changes on the active layer (ADR 0079, ADR 0080). Cells off the canvas are
/// skipped, and the selection keeps the rest to the selected cells.
/// </summary>
internal sealed class ShapeCells
{
    private readonly Layer _layer;
    private readonly CanvasSize _canvas;
    private readonly CellEditBuilder _cells;
    private readonly CellColor _foreground;
    private readonly CellColor _background;

    public ShapeCells(EditingSession session, CellColor foreground, CellColor background)
    {
        _layer = session.ActiveLayer;
        _canvas = session.Document.CanvasSize;
        _cells = session.EditCellsInSelection(_layer.Id);
        _foreground = foreground;
        _background = background;
    }

    /// <summary>
    /// Puts <paramref name="glyph"/> at <paramref name="point"/> in the colours, as typing does: with no background
    /// chosen, the cell keeps its own.
    /// </summary>
    public void PutGlyph(CellPoint point, Rune glyph)
    {
        if (!_canvas.Contains(point))
            return;

        var cell = _layer.GetCell(point);
        var background = _background.IsUnset ? cell.Background : _background;
        Change(point, cell, Cell.FromGlyph(glyph, _foreground, background));
    }

    /// <summary>
    /// Puts <paramref name="line"/>'s characters in its cells, as <see cref="PutGlyph"/> does. With
    /// <paramref name="joins"/>, each joins the line character already in its cell (ADR 0081); an open line's ends
    /// join only on the side the line comes from, and arms the old character had toward a cell in
    /// <paramref name="covered"/>, which something drawn over, are dropped (ADR 0082).
    /// </summary>
    public void PutLine(IReadOnlyList<LineGlyph> line, bool isClosed, bool joins,
        IReadOnlySet<CellPoint>? covered = null)
    {
        ArgumentNullException.ThrowIfNull(line);
        for (var index = 0; index < line.Count; index++)
        {
            var (point, glyph) = line[index];
            if (joins && _canvas.Contains(point) && _layer.GetCell(point) is { HasGlyph: true } cell)
            {
                var along = LineJoins.Along(line, index, isClosed);
                glyph = LineJoins.Join(glyph, along, cell.Glyph, CoveredSidesOf(point, covered));
            }

            PutGlyph(point, glyph);
        }
    }

    /// <summary>Gives the cell at <paramref name="point"/> the background colour, keeping its glyph.</summary>
    public void PutBackground(CellPoint point)
    {
        if (!_canvas.Contains(point))
            return;

        var cell = _layer.GetCell(point);
        Change(point, cell, cell.WithBackground(_background));
    }

    /// <summary>The step that makes the changes, or null when no cell would change.</summary>
    public CellEditCommand? Build(string stepNameKey) => _cells.Build(stepNameKey);

    private static LineSides CoveredSidesOf(CellPoint point, IReadOnlySet<CellPoint>? covered)
    {
        if (covered is null)
            return LineSides.None;

        var sides = LineSides.None;
        if (covered.Contains(new CellPoint(point.X, point.Y - 1)))
            sides |= LineSides.Up;

        if (covered.Contains(new CellPoint(point.X + 1, point.Y)))
            sides |= LineSides.Right;

        if (covered.Contains(new CellPoint(point.X, point.Y + 1)))
            sides |= LineSides.Down;

        if (covered.Contains(new CellPoint(point.X - 1, point.Y)))
            sides |= LineSides.Left;

        return sides;
    }

    private void Change(CellPoint point, Cell cell, Cell changed)
    {
        if (changed != cell)
            _cells.Set(point, changed);
    }
}
