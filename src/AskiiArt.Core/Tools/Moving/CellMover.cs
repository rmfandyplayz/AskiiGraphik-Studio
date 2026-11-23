// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Clipboard;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Selection;

namespace AskiiArt.Core.Tools.Moving;

/// <summary>
/// A move of the active layer's selected cells, or of the whole layer while nothing is selected, whose cells float
/// until it ends (ADRs 0066, 0070). The Move tool's drags carry it on, and the arrows do, in the Move tool and in the
/// selection tools alike (ADR 0073): one cell per arrow and ten with Shift, a run of presses making one step and each
/// Shift and arrow its own. The selection can go as far as it still touches the zone around the canvas (ADR 0072).
/// An undo or redo of the move's own steps carries it on from where they put its cells; any other change, or another
/// active layer, ends it where its cells are.
/// </summary>
internal sealed class CellMover
{
    // How many cells Shift and an arrow move the cells.
    private const int FarStep = 10;

    private readonly EditingSession _session;
    private readonly Func<double> _cellWidthToHeight;
    private readonly NudgeRun _nudges;

    public CellMover(EditingSession session, Func<double> cellWidthToHeight)
    {
        _session = session;
        _cellWidthToHeight = cellWidthToHeight;
        _nudges = new NudgeRun(session.Clock);
        _session.History.StateChanged += OnHistoryChanged;
        _session.ActiveLayerChanged += OnActiveLayerChanged;
    }

    /// <summary>
    /// Raised when something other than the gesture's owner ended a gesture under way: an undo or redo, or the move
    /// itself ending.
    /// </summary>
    public event EventHandler? GestureInterrupted;

    /// <summary>The move under way, or null.</summary>
    public FloatingMove? Move { get; private set; }

    /// <summary>True while a move is under way and has made a step: its cells float, or a paste's do.</summary>
    public bool IsMoving => Move is { HasStep: true };

    /// <summary>True for the keys that nudge the cells: the arrows, and the arrows with Shift.</summary>
    public static bool IsArrow(ToolKey key) => NudgeOf(key) is not null;

    /// <summary>
    /// Starts a gesture, such as a drag, that carries on the move under way, or starts a move. With
    /// <paramref name="copies"/> it always starts a new one that moves a copy, ending the move before it.
    /// </summary>
    public FloatingMove BeginGesture(bool copies)
    {
        _nudges.Stop();
        if (copies || Move is null)
        {
            End();
            Move = new FloatingMove(_session, copies, Zone());
        }

        Move.BeginGesture();
        return Move;
    }

    /// <summary>
    /// Nudges the cells as <paramref name="key"/> says, starting a move if none is under way.
    /// </summary>
    /// <returns>False, moving nothing, when the key isn't an arrow or the active layer is locked or hidden.</returns>
    public bool Nudge(ToolKey key)
    {
        if (NudgeOf(key) is not { } nudge || !_session.CanEditActiveLayer)
            return false;

        var move = Move ??= new FloatingMove(_session, isCopy: false, Zone());
        var target = move.Limit(new CellPoint(move.Offset.X + nudge.X, move.Offset.Y + nudge.Y));
        if (IsFar(key))
        {
            _nudges.Stop();
            move.BeginGesture();
            move.MoveTo(target);
            move.EndGesture();
            return true;
        }

        if (!_nudges.Continues(key, nudge) || !move.IsInGesture)
            move.BeginGesture();

        move.MoveTo(target);
        return true;
    }

    /// <summary>Notes when an arrow is let go, which decides whether the next press starts a new step.</summary>
    public void LetGo(ToolKey arrow) => _nudges.LetGo(arrow);

    /// <summary>Ends a run of arrow presses, so the next press starts a new step.</summary>
    public void StopNudging() => _nudges.Stop();

    /// <summary>Ends the move under way, then floats <paramref name="clip"/> with its top-left corner there.</summary>
    public void Paste(CellClip clip, CellPoint topLeft)
    {
        End();
        Move = FloatingMove.Paste(_session, clip, topLeft, Zone());
    }

    /// <summary>Ends the move: its cells stay where they are, and stop floating.</summary>
    public void End()
    {
        var hadMove = Move is not null;
        Move?.EndGesture();
        Move = null;
        _nudges.Stop();
        if (hadMove)
            GestureInterrupted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Ends the move and deselects, as Enter does.</summary>
    /// <returns>True when there was a move whose cells floated, or a selection.</returns>
    public bool Finish()
    {
        var wasMoving = IsMoving;
        End();
        return SelectionEdits.Deselect(_session) || wasMoving;
    }

    /// <summary>Ends the move and stops following the session, for a tool being put away.</summary>
    public void Detach()
    {
        End();
        _session.History.StateChanged -= OnHistoryChanged;
        _session.ActiveLayerChanged -= OnActiveLayerChanged;
    }

    // The canvas and the zone around it as the cells look now, which the selection must still touch (ADR 0072).
    private CellRect Zone() => SelectionZone.Around(_session.Document.CanvasSize, _cellWidthToHeight());

    // An undo or redo of the move's own steps carries it on from where they put its cells, ending a drag or run of
    // nudges under way. Any other change ends the move where its cells are, since they no longer stand on what the move
    // took them from. A save leaves the history's steps as they were, so the move goes on.
    private void OnHistoryChanged(object? sender, EventArgs e)
    {
        if (Move is null || Move.IsChanging)
            return;

        if (!Move.FollowHistory())
        {
            End();
            return;
        }

        if (Move.IsInGesture)
            return;

        _nudges.Stop();
        GestureInterrupted?.Invoke(this, EventArgs.Empty);
    }

    private void OnActiveLayerChanged(object? sender, EventArgs e) => End();

    private static bool IsFar(ToolKey key) =>
        key is ToolKey.FarLeft or ToolKey.FarRight or ToolKey.FarUp or ToolKey.FarDown;

    private static CellPoint? NudgeOf(ToolKey key) => key switch
    {
        ToolKey.Left => new CellPoint(-1, 0),
        ToolKey.Right => new CellPoint(1, 0),
        ToolKey.Up => new CellPoint(0, -1),
        ToolKey.Down => new CellPoint(0, 1),
        ToolKey.FarLeft => new CellPoint(-FarStep, 0),
        ToolKey.FarRight => new CellPoint(FarStep, 0),
        ToolKey.FarUp => new CellPoint(0, -FarStep),
        ToolKey.FarDown => new CellPoint(0, FarStep),
        _ => null
    };
}
