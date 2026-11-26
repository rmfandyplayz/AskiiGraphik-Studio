// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Selection;

namespace AskiiArt.Core.Tools.Selecting;

/// <summary>
/// A drag from one cell to another that selects a shape fitted to the box between them, for Rectangle Select and
/// Ellipse Select (ADR 0055, ADR 0060). Shift or Alt held when the drag starts picks the mode. Pressed during the drag,
/// Shift makes the box look square on screen and Alt grows it from the first cell outwards; one held from the start
/// does this once it's let go and pressed again.
/// </summary>
internal sealed class BoxSelectGesture
{
    private readonly EditingSession _session;
    private readonly SelectionCombiner _combiner;
    private readonly Func<double> _cellWidthToHeight;
    private readonly Func<CanvasSize, CellRect, SelectionMask> _shape;
    private readonly string _stepNameKey;
    private readonly SelectionArrows _arrows;
    private ToolOverlay _overlay = ToolOverlay.None;

    // Where the drag started, or null when there's no drag, and the cell it has reached.
    private CellPoint? _anchor;
    private CellPoint _corner;
    private ToolModifiers _modifiers;

    // The modifiers held since the drag started, which chose the mode rather than shape the box.
    private ToolModifiers _choseTheMode;

    // Whether the drag has left its first cell, which makes it a drag rather than a click.
    private bool _hasMoved;

    public BoxSelectGesture(ToolContext context, Func<CanvasSize, CellRect, SelectionMask> shape, string stepNameKey)
    {
        ArgumentNullException.ThrowIfNull(context);
        _session = context.Session;
        _combiner = new SelectionCombiner(context.Session, context.Settings.For(SelectionToolOptions.Group),
            ShowPreview);
        _cellWidthToHeight = context.CellWidthToHeight;
        _shape = shape;
        _stepNameKey = stepNameKey;
        _arrows = new SelectionArrows(context);
    }

    public event EventHandler? OverlayChanged;

    public ToolOverlay Overlay => _overlay;

    public void Press(ToolPointerInput input)
    {
        _arrows.EndMove();
        _anchor = input.Cell;
        _corner = input.Cell;
        _modifiers = input.Modifiers;
        _choseTheMode = input.Modifiers & (ToolModifiers.Shift | ToolModifiers.Alt);
        _hasMoved = false;
        _combiner.Start(input.Modifiers);
    }

    public void Move(ToolPointerInput input) => Update(input.Cell, input.Modifiers);

    public void Release(ToolPointerInput input)
    {
        if (_anchor is null)
            return;

        Update(input.Cell, input.Modifiers);
        var shape = _hasMoved ? Shape() : null;
        End();
        _combiner.Finish(shape, _stepNameKey);
    }

    public void ModifiersChanged(ToolModifiers modifiers) => Update(_corner, modifiers);

    public bool KeyPressed(ToolKey key)
    {
        if (key != ToolKey.Escape || _anchor is null)
            return _arrows.KeyPressed(key, isGestureUnderWay: _anchor is not null);

        End();
        return true;
    }

    public void ArrowReleased(ToolKey arrow) => _arrows.ArrowReleased(arrow);

    public void Deactivate()
    {
        End();
        _arrows.Detach();
    }

    private void Update(CellPoint cell, ToolModifiers modifiers)
    {
        if (_anchor is not { } anchor)
            return;

        // A key let go no longer chooses the mode, so pressing it again shapes the box.
        _choseTheMode &= modifiers;
        if (cell == _corner && modifiers == _modifiers)
            return;

        _corner = cell;
        _modifiers = modifiers;
        _hasMoved |= cell != anchor;
        if (!_hasMoved)
            return;

        // The canvas works the shape out only if it draws this overlay.
        var box = Box();
        SetOverlay(new ToolOverlay(Caret: null,
            SelectionPreview: _combiner.Preview(() => _shape(_session.Document.CanvasSize, box))));
    }

    private SelectionMask Shape() => _shape(_session.Document.CanvasSize, Box());

    private CellRect Box()
    {
        var shaping = _modifiers & ~_choseTheMode;
        return DragBox.Between(_anchor ?? _corner, _corner, shaping.HasFlag(ToolModifiers.Shift),
            shaping.HasFlag(ToolModifiers.Alt), _cellWidthToHeight());
    }

    private void End()
    {
        _anchor = null;
        _hasMoved = false;
        _combiner.Stop();
        SetOverlay(ToolOverlay.None);
    }

    // An undo or redo during the drag changed the selection the box combines with.
    private void ShowPreview(SelectionPreview preview) => SetOverlay(_overlay with { SelectionPreview = preview });

    private void SetOverlay(ToolOverlay overlay)
    {
        if (overlay == _overlay)
            return;

        _overlay = overlay;
        OverlayChanged?.Invoke(this, EventArgs.Empty);
    }
}
