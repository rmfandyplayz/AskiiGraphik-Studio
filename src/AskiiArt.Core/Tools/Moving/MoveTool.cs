// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Clipboard;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Selection;

namespace AskiiArt.Core.Tools.Moving;

/// <summary>
/// The Move tool (ARCHITECTURE §10, ADR 0066). A drag that starts anywhere on the canvas moves the selected cells of
/// the active layer, or the whole layer when nothing is selected, a cell at a time, and the selection goes with them.
/// The arrow keys nudge them one cell, and Shift and an arrow ten. Alt+drag (Option+drag on a Mac) moves a copy.
/// During a drag, Shift keeps the move to left and right or up and down, Ctrl (Cmd on a Mac) to the diagonals that look
/// like 45°, and Ctrl+Shift to the diagonals of one column per row (ADR 0069). A click that stays in an
/// unselected cell deselects when the button comes up, as Rectangle Select's does.
/// <para>
/// Each drag is an undo step, and so is each run of arrow presses and each Shift and arrow (ADR 0070). The moved cells
/// float: over several drags and nudges, and undos and redos of them, they pass over other art without harming it.
/// Enter ends the move and deselects, and another tool, another layer or any other change ends it too; then the cells
/// replace what they're on, though empty ones don't. Esc during a drag puts the cells back where the drag found them.
/// The selection can go past the canvas's edges for as long as it still touches the zone around it (ADRs 0071, 0072),
/// and stays there when the move ends, while cells moved off the canvas are kept only while the move lasts. Moving the whole layer selects it as it moves.
/// It does nothing on a locked or hidden layer.
/// </para>
/// </summary>
public sealed class MoveTool : ITool
{
    /// <summary>The tool's id.</summary>
    public const string Id = "askiiart.move";

    private readonly EditingSession _session;
    private readonly Func<double> _cellWidthToHeight;
    private readonly CellMover _mover;

    // Where the drag started, and how far the cells had moved then; no cell while no drag is under way.
    private CellPoint? _pressCell;
    private CellPoint _offsetAtPress;

    // Where the pointer is during the drag, and the modifiers held, which keep the move to a few directions.
    private CellPoint _pointerCell;
    private ToolModifiers _modifiers;

    // Whether the drag has left its first cell, which makes it a drag rather than a click, and whether that cell was
    // outside the selection, where a click deselects.
    private bool _hasLeftPressCell;
    private bool _pressedOutsideSelection;

    /// <summary>Creates the tool for <paramref name="context"/>'s session.</summary>
    public MoveTool(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _session = context.Session;
        _cellWidthToHeight = context.CellWidthToHeight;
        _mover = new CellMover(_session, _cellWidthToHeight);

        // An undo or redo, or the move ending, ends a drag under way.
        _mover.GestureInterrupted += (_, _) => _pressCell = null;
    }

    /// <summary>The Move tool draws nothing over the document, so this never happens.</summary>
    public event EventHandler? OverlayChanged
    {
        add { }
        remove { }
    }

    /// <summary>The tool's identity. The name key is the App's resource key.</summary>
    public static ToolDescriptor ToolDescriptor { get; } = new(Id, "ToolMove", "tool.move", "select");

    /// <summary>
    /// How long after the last arrow was let go another press still carries a run of presses on, as one step
    /// (ADR 0070). The canvas waits as long before the view settles after arrows (ADR 0072).
    /// </summary>
    public static TimeSpan ArrowRunPause => NudgeRun.Pause;

    /// <summary>Registers the Move tool, as DI does.</summary>
    public static ToolDefinition Definition { get; } = new(ToolDescriptor, context => new MoveTool(context));

    /// <inheritdoc/>
    public ToolDescriptor Descriptor => ToolDescriptor;

    /// <summary>True: Space + drag pans.</summary>
    public bool AllowsSpacePanning => true;

    /// <summary>False: dragging moves cells.</summary>
    public bool PansWithPrimaryButton => false;

    /// <summary>False: the tool takes no text.</summary>
    public bool IsTakingText => false;

    /// <summary>Nothing: the document itself shows the move.</summary>
    public ToolOverlay Overlay => ToolOverlay.None;

    /// <summary>True while a move is under way and has made a step: its cells float, or a paste's do.</summary>
    public bool IsMoving => _mover.IsMoving;

    /// <summary>
    /// Over the selected cells, which a drag moves, or anywhere on the canvas while nothing is selected, as a drag then
    /// moves the whole layer. Never while the active layer is locked or hidden (ADR 0067).
    /// </summary>
    public bool ShowsMoveCursorAt(CellPoint cell)
    {
        if (!_session.CanEditActiveLayer)
            return false;

        var selection = _session.Selection;
        return selection.IsEmpty ? _session.Document.CanvasSize.Contains(cell) : selection.Mask.Contains(cell);
    }

    /// <summary>
    /// Starts a drag that carries on the move under way, or starts one. With Alt it always starts one that moves a
    /// copy, which ends the move before it where its cells are.
    /// </summary>
    public void PointerPressed(ToolPointerInput input)
    {
        _pressCell = null;
        _mover.StopNudging();
        if (!_session.CanEditActiveLayer)
            return;

        var move = _mover.BeginGesture(copies: input.Modifiers.HasFlag(ToolModifiers.Alt));
        _pressCell = input.Cell;
        _offsetAtPress = move.Offset;
        _pointerCell = input.Cell;
        _modifiers = input.Modifiers;
        _hasLeftPressCell = false;
        _pressedOutsideSelection = !_session.Selection.IsEmpty && !_session.Selection.Mask.Contains(input.Cell);
    }

    /// <summary>
    /// During a drag, moves the cells as far as the pointer has come from where the drag started, kept to a few
    /// directions while Shift or Ctrl is held.
    /// </summary>
    public void PointerMoved(ToolPointerInput input)
    {
        if (_pressCell is not { } press)
            return;

        _pointerCell = input.Cell;
        _modifiers = input.Modifiers;
        _hasLeftPressCell |= input.Cell != press;
        FollowPointer();
    }

    /// <summary>
    /// Ends the drag and its step. The move goes on, so the next drag or nudge carries it on. A click that stayed in an
    /// unselected cell ends the move and deselects instead.
    /// </summary>
    public void PointerReleased(ToolPointerInput input)
    {
        if (_pressCell is null)
            return;

        PointerMoved(input);
        var deselects = !_hasLeftPressCell && _pressedOutsideSelection;
        _pressCell = null;
        _mover.Move?.EndGesture();
        if (!deselects)
            return;

        _mover.End();
        SelectionEdits.Deselect(_session);
    }

    /// <summary>Shift or Ctrl going down or up during a drag changes where the cells are at once.</summary>
    public void ModifiersChanged(ToolModifiers modifiers)
    {
        if (_pressCell is null)
            return;

        _modifiers = modifiers;
        FollowPointer();
    }

    /// <summary>
    /// The arrows nudge the cells, also when no move is under way, a run of presses making one step and each Shift and
    /// arrow its own. Enter ends the move and deselects. Esc during a drag puts the cells back where it found them;
    /// otherwise it isn't the tool's, so the App switches to the Hand tool (ADR 0041).
    /// </summary>
    public bool KeyPressed(ToolKey key, ToolModifiers modifiers)
    {
        if (key == ToolKey.Escape)
            return CancelDrag();

        if (key == ToolKey.Enter)
            return _mover.Finish();

        if (!CellMover.IsArrow(key))
            return false;

        // A nudge during a drag would fight the pointer.
        return _pressCell is not null || _mover.Nudge(key);
    }

    /// <summary>Notes when an arrow is let go, which decides whether the next press starts a new step.</summary>
    public void ArrowReleased(ToolKey arrow) => _mover.LetGo(arrow);

    /// <summary>Takes no text.</summary>
    public bool TextEntered(string text) => false;

    /// <summary>
    /// Pastes <paramref name="clip"/> with its top-left corner at <paramref name="topLeft"/> (ADR 0067). Its cells
    /// float on the active layer, selected, as a move's do, so drags and nudges carry them on as one Paste step until
    /// the move ends, and Esc takes the paste away. A move under way ends first.
    /// </summary>
    /// <returns>False, pasting nothing, when the active layer is locked or hidden.</returns>
    public bool Paste(CellClip clip, CellPoint topLeft)
    {
        ArgumentNullException.ThrowIfNull(clip);

        if (!_session.CanEditActiveLayer)
            return false;

        _mover.Paste(clip, topLeft);
        return true;
    }

    /// <summary>Ends the move where its cells are.</summary>
    public void Deactivate() => _mover.Detach();

    private void FollowPointer()
    {
        if (_pressCell is not { } press || _mover.Move is not { } move)
            return;

        var distance = MoveConstraint.Apply(new CellPoint(_pointerCell.X - press.X, _pointerCell.Y - press.Y),
            _modifiers, _cellWidthToHeight());
        move.MoveTo(move.Limit(new CellPoint(_offsetAtPress.X + distance.X, _offsetAtPress.Y + distance.Y)));
    }

    private bool CancelDrag()
    {
        if (_pressCell is null || _mover.Move is not { } move)
            return false;

        move.CancelGesture();
        _pressCell = null;
        return true;
    }
}
