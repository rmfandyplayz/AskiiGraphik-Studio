// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Selection;

namespace AskiiArt.Core.Tools.Selecting;

/// <summary>
/// Rectangle Select (ARCHITECTURE §10, ADR 0055): dragging from one cell to another selects the rectangle between
/// them, both included, in place of the selection; a click that doesn't move to another cell selects nothing. The
/// canvas outlines the rectangle while it's dragged, and releasing makes it one undo step; Esc during the drag cancels
/// it. It only selects, so it works on a locked or hidden layer too.
/// </summary>
public sealed class RectangleSelectTool : ITool
{
    /// <summary>The tool's id.</summary>
    public const string Id = "askiiart.select.rectangle";

    private readonly EditingSession _session;
    private ToolOverlay _overlay = ToolOverlay.None;

    // Where the drag started, or null when there's no drag, and the cell it has reached.
    private CellPoint? _anchor;
    private CellPoint _corner;

    // Whether the drag has left its first cell, which makes it a drag rather than a click.
    private bool _hasMoved;

    /// <summary>Creates the tool for <paramref name="session"/>.</summary>
    public RectangleSelectTool(EditingSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
    }

    /// <summary>Raised when a drag starts showing a rectangle, the rectangle changes, or the drag ends.</summary>
    public event EventHandler? OverlayChanged;

    /// <summary>The tool's identity. The name key is the App's resource key.</summary>
    public static ToolDescriptor ToolDescriptor { get; } =
        new(Id, "ToolRectangleSelect", "tool.select.rectangle", "select");

    /// <summary>Registers Rectangle Select, as DI does.</summary>
    public static ToolDefinition Definition { get; } =
        new(ToolDescriptor, context => new RectangleSelectTool(context.Session));

    /// <inheritdoc/>
    public ToolDescriptor Descriptor => ToolDescriptor;

    /// <summary>True: Space + drag pans.</summary>
    public bool AllowsSpacePanning => true;

    /// <summary>False: dragging selects.</summary>
    public bool PansWithPrimaryButton => false;

    /// <summary>False: the tool takes no text.</summary>
    public bool IsTakingText => false;

    /// <summary>The rectangle being dragged, as the selection it would make.</summary>
    public ToolOverlay Overlay => _overlay;

    /// <summary>Starts a drag. The selection stays as it is until the pointer reaches another cell.</summary>
    public void PointerPressed(ToolPointerInput input)
    {
        _anchor = input.Cell;
        _corner = input.Cell;
        _hasMoved = false;
    }

    /// <summary>Stretches the rectangle to the cell under the pointer, which may be off the canvas.</summary>
    public void PointerMoved(ToolPointerInput input) => MoveCorner(input.Cell);

    /// <summary>Selects the rectangle, or nothing after a click, as one undo step.</summary>
    public void PointerReleased(ToolPointerInput input)
    {
        if (_anchor is null)
            return;

        MoveCorner(input.Cell);
        var mask = _hasMoved ? DraggedMask() : null;
        EndDrag();

        if (mask is null || mask.IsEmpty)
            SelectionEdits.Deselect(_session);
        else
            SelectionEdits.Select(_session, mask, UndoStepNames.RectangleSelect);
    }

    /// <summary>
    /// Esc during a drag cancels it, leaving the selection as it was. Otherwise Esc isn't the tool's, so the App
    /// switches to the Hand tool (ADR 0041).
    /// </summary>
    public bool KeyPressed(ToolKey key, ToolModifiers modifiers)
    {
        if (key != ToolKey.Escape || _anchor is null)
            return false;

        EndDrag();
        return true;
    }

    /// <summary>Takes no text.</summary>
    public bool TextEntered(string text) => false;

    /// <summary>Cancels a drag in progress, leaving the selection as it was.</summary>
    public void Deactivate() => EndDrag();

    private void MoveCorner(CellPoint cell)
    {
        if (_anchor is not { } anchor || cell == _corner)
            return;

        _corner = cell;
        _hasMoved |= cell != anchor;
        if (_hasMoved)
            SetOverlay(new ToolOverlay(Caret: null, PendingSelection: DraggedMask()));
    }

    private SelectionMask DraggedMask()
    {
        var anchor = _anchor ?? _corner;
        var rect = CellRect.FromEdges(
            Math.Min(anchor.X, _corner.X),
            Math.Min(anchor.Y, _corner.Y),
            Math.Max(anchor.X, _corner.X) + 1,
            Math.Max(anchor.Y, _corner.Y) + 1);
        return SelectionMask.Rectangle(_session.Document.CanvasSize, rect);
    }

    private void EndDrag()
    {
        _anchor = null;
        _hasMoved = false;
        SetOverlay(ToolOverlay.None);
    }

    private void SetOverlay(ToolOverlay overlay)
    {
        if (overlay == _overlay)
            return;

        _overlay = overlay;
        OverlayChanged?.Invoke(this, EventArgs.Empty);
    }
}
