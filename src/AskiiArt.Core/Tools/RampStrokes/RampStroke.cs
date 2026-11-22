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
/// under the tip moves from its darkness before the stroke towards <c>target</c>, by the tip's weight there times the
/// pen's pressure (ADR 0054) times <c>strength</c>. A cell's darkness is its level in the ramp
/// (<see cref="Ramp.NearestLevel"/>), and an empty cell's is 0. A cell moves once, by the greatest weight that has
/// reached it, so going over it again adds nothing, pressing harder takes it further, and the next stroke takes it
/// further. The new level is the nearest one, unless the stroke moves at least a level: then any
/// move short of a level still takes the cell one level towards the target. Its edits show as it goes and coalesce
/// into one undo step.
/// <para>
/// Where the tip's weight changes across a cell, as at its edge, each part of the cell moves by its own weight, and
/// the cell gets the level, between where it was and the target, whose glyph best shows those parts
/// (<see cref="RampShapeMatcher"/>, ADR 0068), so the ink lies where the tip painted. A part moves once too, by the
/// greatest weight that has reached it.
/// </para>
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

    // Matches the ramp's glyphs to cells the tip weighs unevenly; made when the stroke first reaches one.
    private RampShapeMatcher? _matcher;

    private CellPoint _lastTopLeft;

    // The hardest the pen has pressed since the tip got to where it is.
    private double _hardestPressureHere;

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
        _hardestPressureHere = PressureOf(start);
        Move(_lastTopLeft, _lastTopLeft, _hardestPressureHere);
    }

    /// <summary>The tip the stroke started with, which it keeps throughout.</summary>
    public BrushFalloff Falloff { get; }

    /// <summary>The cells the tip covers.</summary>
    public BrushFootprint Footprint => Falloff.Footprint;

    /// <summary>
    /// Continues the stroke in a straight line, because a fast drag reports cells far apart, at the pressure
    /// <paramref name="input"/> reports. A pen pressed harder without moving reaches further where it is.
    /// </summary>
    public void MoveTo(ToolPointerInput input)
    {
        var topLeft = Footprint.TopLeftAt(input);
        var pressure = PressureOf(input);
        if (topLeft == _lastTopLeft)
        {
            if (pressure <= _hardestPressureHere)
                return;

            _hardestPressureHere = pressure;
            Move(topLeft, topLeft, pressure);
            return;
        }

        Move(_lastTopLeft, topLeft, pressure);
        _lastTopLeft = topLeft;
        _hardestPressureHere = pressure;
    }

    /// <summary>Takes the stroke back as if it never happened, unless another step has followed it.</summary>
    public void Cancel()
    {
        if (_undoCountAfterLastEdit == _session.History.UndoCount)
            _session.History.CancelNewestStep();
    }

    // The whole way from the last report takes this report's pressure, since a pen reports often enough.
    private void Move(CellPoint fromTopLeft, CellPoint toTopLeft, double pressure)
    {
        // Undoing during a stroke can take away the layer it started on, or lock it.
        if (!_session.CanEditActiveLayer)
            return;

        var document = _session.Document;
        var layer = _session.ActiveLayer;
        var cells = _session.EditCellsInSelection(layer.Id);
        foreach (var (point, tipWeight, tipParts) in FootprintSweep.CoverWeighted(Falloff, fromTopLeft, toTopLeft,
                     document.CanvasSize))
        {
            var now = layer.GetCell(point);
            if (!_reached.TryGetValue(point, out var before))
                before = new ReachedCell(now, 0, null);

            if (!TryRaise(before, tipWeight * pressure, tipParts, pressure, out var reached))
                continue;

            _reached[point] = reached;
            var moved = MoveCell(reached);
            if (moved != now)
                cells.Set(point, moved);
        }

        if (cells.Build(_stepNameKey) is not { } command)
            return;

        _session.History.Execute(command, _undo);
        _undoCountAfterLastEdit = _session.History.UndoCount;
    }

    // The cell with its weights raised to those of a stamp, or false when the stamp raises none of them. Parts left
    // too nearly even to matter become a weight for the whole cell, their average.
    private static bool TryRaise(ReachedCell before, double weight, float[]? parts, double pressure,
        out ReachedCell raised)
    {
        var newWeight = Math.Max(before.Weight, weight);
        raised = before with { Weight = newWeight };
        if (parts is null && before.Parts is null)
            return newWeight > before.Weight;

        var floor = (float)newWeight;
        var isRaised = false;
        for (var part = 0; part < GlyphShape.PartCount; part++)
            isRaised |= RaisedPart(before, weight, parts, pressure, part, floor) > OldPart(before, part);

        if (!isRaised)
            return newWeight > before.Weight;

        // Only the stroke holds a cell's parts, and it replaces them with these, so they can be raised where they are.
        var newParts = before.Parts ?? new float[GlyphShape.PartCount];
        var lightest = float.PositiveInfinity;
        var heaviest = float.NegativeInfinity;
        for (var part = 0; part < newParts.Length; part++)
        {
            newParts[part] = RaisedPart(before, weight, parts, pressure, part, floor);
            lightest = Math.Min(lightest, newParts[part]);
            heaviest = Math.Max(heaviest, newParts[part]);
        }

        raised = heaviest - lightest <= BrushFalloff.EvenSpread
            ? new ReachedCell(before.Cell, newParts.Average(), null)
            : new ReachedCell(before.Cell, newWeight, newParts);
        return true;
    }

    private static float OldPart(ReachedCell cell, int part) => cell.Parts?[part] ?? (float)cell.Weight;

    private static float RaisedPart(ReachedCell before, double weight, float[]? parts, double pressure, int part,
        float floor)
    {
        var stamped = parts is null ? (float)weight : parts[part] * (float)pressure;
        return Math.Max(Math.Max(OldPart(before, part), stamped), floor);
    }

    private Cell MoveCell(ReachedCell reached)
    {
        var before = reached.Cell;
        var levelBefore = LevelOf(before);
        var darknessBefore = _ramp.IntensityOf(levelBefore);
        int level;
        double change;
        if (reached.Parts is null)
        {
            change = (_target - darknessBefore) * reached.Weight * _strength;
            level = _ramp.LevelAt(darknessBefore + change);
        }
        else
        {
            level = MatchedLevel(reached.Parts, levelBefore, darknessBefore, out change);
        }

        // A cell short of the target's level is always on the target's side of it, so one level can't overshoot.
        if (_movesAtLeastOneLevel && level == levelBefore && level != _targetLevel && change != 0)
            level += Math.Sign(change);

        return _paint(before, levelBefore, level);
    }

    // The level, between the cell's and the target's, whose glyph best shows each part moved by its own weight.
    // Change is how far the parts moved on average.
    private int MatchedLevel(float[] parts, int levelBefore, double darknessBefore, out double change)
    {
        Span<double> target = stackalloc double[GlyphShape.PartCount];
        var totalChange = 0.0;
        for (var part = 0; part < target.Length; part++)
        {
            var partChange = (_target - darknessBefore) * parts[part] * _strength;
            target[part] = darknessBefore + partChange;
            totalChange += partChange;
        }

        change = totalChange / target.Length;
        _matcher ??= new RampShapeMatcher(_ramp, _densities());
        return _matcher.LevelFor(target, Math.Min(levelBefore, _targetLevel), Math.Max(levelBefore, _targetLevel));
    }

    // A pressure the device can't tell counts as full, as a mouse's does.
    private static double PressureOf(ToolPointerInput input) =>
        float.IsNaN(input.Pressure) ? 1 : Math.Clamp(input.Pressure, 0, 1);

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

    // A cell the stroke has reached: as it was before the stroke, the greatest weight that has reached it as a whole,
    // and where the tip weighed it unevenly, the greatest weight of each part, each at least Weight; else null.
    private readonly record struct ReachedCell(Cell Cell, double Weight, float[]? Parts);
}
