// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Ramps;
using AskiiArt.Core.Tools.Footprints;

namespace AskiiArt.Core.Tools.RampStrokes;

/// <summary>Gives a cell a ramp stroke has moved. Return <paramref name="before"/> to leave it as it was.</summary>
/// <param name="before">The cell as it was before the stroke.</param>
/// <param name="levelBefore">Its level in the ramp before the stroke.</param>
/// <param name="level">The level the stroke moved it to.</param>
internal delegate Cell RampCellPaint(Cell before, int levelBefore, int level);

/// <summary>
/// One stroke of a tool that moves cells along a ramp: the Brush, or the Soft Eraser (ADRs 0049, 0050). Each cell
/// under the tip moves from its darkness before the stroke towards <c>target</c>, by the tip's weight there times
/// <c>strength</c>. A cell's darkness is its level in the ramp (<see cref="Ramp.NearestLevel"/>), and an empty cell's
/// is 0. A cell moves once, by the greatest weight that has reached it, so going over it again adds nothing, and the
/// next stroke takes it further. The new level is the nearest one, unless the stroke moves at least a level: then any
/// move short of a level still takes the cell one level towards the target. Its edits show as it goes and coalesce
/// into one undo step.
/// </summary>
internal sealed class RampStroke
{
    private readonly EditingSession _session;
    private readonly Ramp _ramp;
    private readonly double _target;
    private readonly double _strength;
    private readonly bool _movesAtLeastOneLevel;
    private readonly int _targetLevel;
    private readonly Func<IGlyphDensityProvider> _densities;
    private readonly RampCellPaint _paint;
    private readonly string _stepNameKey;
    private readonly CoalescingGroup _undo = new();
    private readonly Dictionary<CellPoint, ReachedCell> _reached = [];

    // Each glyph's level, measured once per stroke.
    private readonly Dictionary<Rune, int> _levels = [];

    private CellPoint _lastTopLeft;

    // Where the history stood after this stroke's last edit, or null before its first. Esc only takes back the newest
    // step if it's still this stroke's.
    private int? _undoCountAfterLastEdit;

    /// <summary>Starts a stroke at <paramref name="start"/>, and moves the cells under the tip there.</summary>
    /// <param name="session">The session whose active layer the stroke changes.</param>
    /// <param name="ramp">The ramp cells move along.</param>
    /// <param name="falloff">The tip, and how strongly it reaches each of its cells.</param>
    /// <param name="target">The darkness cells move towards, from 0 to 1.</param>
    /// <param name="strength">How far, from 0 to 1, the full weight moves a cell to <paramref name="target"/>.</param>
    /// <param name="movesAtLeastOneLevel">
    /// Whether a cell the stroke moves at all, by less than a level, moves one level, rather than to the nearest.
    /// </param>
    /// <param name="densities">How dark glyphs look in the document's font, for glyphs the ramp doesn't hold.</param>
    /// <param name="paint">Gives each cell that moved.</param>
    /// <param name="stepNameKey">Names the undo step: a key from <see cref="UndoStepNames"/>.</param>
    /// <param name="start">Where the pointer was pressed.</param>
    public RampStroke(EditingSession session, Ramp ramp, BrushFalloff falloff, double target, double strength,
        bool movesAtLeastOneLevel, Func<IGlyphDensityProvider> densities, RampCellPaint paint, string stepNameKey,
        ToolPointerInput start)
    {
        _session = session;
        _ramp = ramp;
        Falloff = falloff;
        _target = target;
        _strength = strength;
        _movesAtLeastOneLevel = movesAtLeastOneLevel;
        _targetLevel = ramp.LevelAt(target);
        _densities = densities;
        _paint = paint;
        _stepNameKey = stepNameKey;

        _lastTopLeft = Footprint.TopLeftAt(start);
        Move(_lastTopLeft, _lastTopLeft);
    }

    /// <summary>The tip the stroke started with, which it keeps throughout.</summary>
    public BrushFalloff Falloff { get; }

    /// <summary>The cells the tip covers.</summary>
    public BrushFootprint Footprint => Falloff.Footprint;

    /// <summary>Continues the stroke in a straight line, because a fast drag reports cells far apart.</summary>
    public void MoveTo(ToolPointerInput input)
    {
        var topLeft = Footprint.TopLeftAt(input);
        if (topLeft == _lastTopLeft)
            return;

        Move(_lastTopLeft, topLeft);
        _lastTopLeft = topLeft;
    }

    /// <summary>Takes the stroke back as if it never happened, unless another step has followed it.</summary>
    public void Cancel()
    {
        if (_undoCountAfterLastEdit == _session.History.UndoCount)
            _session.History.CancelNewestStep();
    }

    private void Move(CellPoint fromTopLeft, CellPoint toTopLeft)
    {
        // Undoing during a stroke can take away the layer it started on, or lock it.
        if (!_session.CanEditActiveLayer)
            return;

        var document = _session.Document;
        var layer = _session.ActiveLayer;
        var cells = new CellEditBuilder(document, layer.Id);
        foreach (var (point, weight) in FootprintSweep.CoverWeighted(Falloff, fromTopLeft, toTopLeft,
                     document.CanvasSize))
        {
            var now = layer.GetCell(point);
            if (!_reached.TryGetValue(point, out var before))
                before = new ReachedCell(now, 0);

            if (weight <= before.Weight)
                continue;

            _reached[point] = before with { Weight = weight };
            var moved = MoveCell(before.Cell, weight);
            if (moved != now)
                cells.Set(point, moved);
        }

        if (cells.Build(_stepNameKey) is not { } command)
            return;

        _session.History.Execute(command, _undo);
        _undoCountAfterLastEdit = _session.History.UndoCount;
    }

    private Cell MoveCell(Cell before, double weight)
    {
        var levelBefore = LevelOf(before);
        var darknessBefore = _ramp.IntensityOf(levelBefore);
        var change = (_target - darknessBefore) * weight * _strength;
        var level = _ramp.LevelAt(darknessBefore + change);

        // A cell short of the target's level is always on the target's side of it, so one level can't overshoot.
        if (_movesAtLeastOneLevel && level == levelBefore && level != _targetLevel && change != 0)
            level += Math.Sign(change);

        return _paint(before, levelBefore, level);
    }

    private int LevelOf(Cell cell)
    {
        if (!cell.HasGlyph)
            return 0;

        if (_levels.TryGetValue(cell.Glyph, out var level))
            return level;

        level = _ramp.NearestLevel(cell.Glyph, _densities());
        _levels.Add(cell.Glyph, level);
        return level;
    }

    // A cell the stroke has reached: as it was before the stroke, and the greatest weight that has reached it.
    private readonly record struct ReachedCell(Cell Cell, double Weight);
}
