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
/// During a drag, Shift keeps the move to left and right or up and down, Ctrl (Cmd on a Mac) to those and diagonals that
/// look like 45°, and Ctrl+Shift to those and diagonals of one column per row (ADR 0069). A click that stays in an
/// unselected cell deselects when the button comes up, as Rectangle Select's does.
/// <para>
/// The moved cells float: over several drags and nudges they pass over other art without harming it, and the whole
/// move is one undo step. Enter, another tool, another layer or any other change ends the move, and then the cells
/// replace what they're on, though empty ones don't. Esc puts the cells back where they began. Cells moved off the
/// canvas are kept while the move lasts, and lost when it ends. Moving the whole layer selects it as it moves. It does
/// nothing on a locked or hidden layer.
/// </para>
/// </summary>
public sealed class MoveTool : ITool
{
    /// <summary>The tool's id.</summary>
    public const string Id = "askiiart.move";

    // How many cells Shift and an arrow move the cells.
    private const int FarStep = 10;

    private readonly EditingSession _session;
    private readonly Func<double> _cellWidthToHeight;
    private FloatingMove? _move;

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
        _session.History.StateChanged += OnHistoryChanged;
        _session.ActiveLayerChanged += OnActiveLayerChanged;
    }

    /// <summary>The Move tool draws nothing over the document, so this never happens.</summary>
    public event EventHandler? OverlayChanged
    {
        add { }
        remove { }
    }

    /// <summary>The tool's identity. The name key is the App's resource key.</summary>
    public static ToolDescriptor ToolDescriptor { get; } = new(Id, "ToolMove", "tool.move", "select");

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

    /// <summary>True while a move is under way: the cells are somewhere other than where it began.</summary>
    public bool IsMoving => _move is { HasStep: true };

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
        if (!_session.CanEditActiveLayer)
            return;

        var copies = input.Modifiers.HasFlag(ToolModifiers.Alt);
        if (copies || _move is null)
            _move = new FloatingMove(_session, copies);

        _pressCell = input.Cell;
        _offsetAtPress = _move.Offset;
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
    /// Ends the drag. The move goes on, so the next drag or nudge carries it on. A click that stayed in an unselected
    /// cell ends the move and deselects instead.
    /// </summary>
    public void PointerReleased(ToolPointerInput input)
    {
        PointerMoved(input);
        var deselects = _pressCell is not null && !_hasLeftPressCell && _pressedOutsideSelection;
        _pressCell = null;
        if (!deselects)
            return;

        EndMove();
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
    /// The arrows nudge the cells, also when no move is under way; Enter ends the move; Esc puts the cells back. Esc
    /// with no move under way isn't the tool's, so the App switches to the Hand tool (ADR 0041).
    /// </summary>
    public bool KeyPressed(ToolKey key, ToolModifiers modifiers)
    {
        if (key == ToolKey.Escape)
            return CancelMove();

        if (key == ToolKey.Enter)
            return EndMove();

        if (NudgeOf(key) is not { } nudge)
            return false;

        // A nudge during a drag would fight the pointer.
        if (_pressCell is not null)
            return true;

        if (!_session.CanEditActiveLayer)
            return false;

        _move ??= new FloatingMove(_session, isCopy: false);
        _move.MoveTo(new CellPoint(_move.Offset.X + nudge.X, _move.Offset.Y + nudge.Y));
        return true;
    }

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

        EndMove();
        _move = FloatingMove.Paste(_session, clip, topLeft);
        return true;
    }

    /// <summary>Ends the move where its cells are.</summary>
    public void Deactivate()
    {
        EndMove();
        _session.History.StateChanged -= OnHistoryChanged;
        _session.ActiveLayerChanged -= OnActiveLayerChanged;
    }

    private void FollowPointer()
    {
        if (_pressCell is not { } press || _move is null)
            return;

        var distance = MoveConstraint.Apply(new CellPoint(_pointerCell.X - press.X, _pointerCell.Y - press.Y),
            _modifiers, _cellWidthToHeight());
        _move.MoveTo(new CellPoint(_offsetAtPress.X + distance.X, _offsetAtPress.Y + distance.Y));
    }

    private bool EndMove()
    {
        var wasMoving = IsMoving;
        _move = null;
        _pressCell = null;
        return wasMoving;
    }

    private bool CancelMove()
    {
        if (!IsMoving)
        {
            EndMove();
            return false;
        }

        _move!.Cancel();
        EndMove();
        return true;
    }

    // An undo, a redo or any other change ends the move where its cells are, since they no longer stand on what the
    // move took them from. A save leaves the history's steps as they were, so the move goes on.
    private void OnHistoryChanged(object? sender, EventArgs e)
    {
        if (_move is { IsChanging: false, IsHistoryAsLeft: false })
            EndMove();
    }

    private void OnActiveLayerChanged(object? sender, EventArgs e) => EndMove();

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
