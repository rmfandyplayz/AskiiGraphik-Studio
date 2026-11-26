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
    /// join only on the side the line comes from.
    /// </summary>
    public void PutLine(IReadOnlyList<LineGlyph> line, bool isClosed, bool joins)
    {
        ArgumentNullException.ThrowIfNull(line);
        for (var index = 0; index < line.Count; index++)
        {
            var (point, glyph) = line[index];
            if (joins && _canvas.Contains(point) && _layer.GetCell(point) is { HasGlyph: true } cell)
                glyph = LineJoins.Join(glyph, LineJoins.Along(line, index, isClosed), cell.Glyph);

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

    private void Change(CellPoint point, Cell cell, Cell changed)
    {
        if (changed != cell)
            _cells.Set(point, changed);
    }
}
