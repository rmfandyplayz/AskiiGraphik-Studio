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
        _combiner = new SelectionCombiner(context.Session, context.Settings.For(SelectionToolOptions.Group));
        _cellWidthToHeight = context.CellWidthToHeight;
        _shape = shape;
        _stepNameKey = stepNameKey;
    }

    public event EventHandler? OverlayChanged;

    public ToolOverlay Overlay => _overlay;

    public void Press(ToolPointerInput input)
    {
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
            return false;

        End();
        return true;
    }

    public void Deactivate() => End();

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
        if (_hasMoved)
            SetOverlay(new ToolOverlay(Caret: null, SelectionPreview: _combiner.Preview(Shape())));
    }

    private SelectionMask Shape() => _shape(_session.Document.CanvasSize, Box());

    private CellRect Box()
    {
        var anchor = _anchor ?? _corner;
        var shaping = _modifiers & ~_choseTheMode;
        var fromCentre = shaping.HasFlag(ToolModifiers.Alt);
        var across = _corner.X - anchor.X;
        var down = _corner.Y - anchor.Y;
        var width = fromCentre ? 2 * Math.Abs(across) + 1 : Math.Abs(across) + 1;
        var height = fromCentre ? 2 * Math.Abs(down) + 1 : Math.Abs(down) + 1;
        if (shaping.HasFlag(ToolModifiers.Shift))
            (width, height) = LookingSquare(width, height, fromCentre);

        if (fromCentre)
            return new CellRect(anchor.X - (width - 1) / 2, anchor.Y - (height - 1) / 2, width, height);

        var left = across >= 0 ? anchor.X : anchor.X - width + 1;
        var top = down >= 0 ? anchor.Y : anchor.Y - height + 1;
        return new CellRect(left, top, width, height);
    }

    // The box grown along its shorter side, as it looks on screen, until it looks square: a cell is usually about
    // twice as tall as it's wide, so a square box has about twice as many columns as rows (ADR 0047's perceptual
    // proportions). From the centre the sides stay odd, so the box stays centred on its first cell.
    private (int Width, int Height) LookingSquare(int width, int height, bool fromCentre)
    {
        var widthToHeight = _cellWidthToHeight();
        if (width * widthToHeight >= height)
            return (width, WholeCells(width * widthToHeight, fromCentre));

        return (WholeCells(height / widthToHeight, fromCentre), height);
    }

    // The nearest whole number of cells to size, at least one, and odd when it must be.
    private static int WholeCells(double size, bool isOdd)
    {
        if (!isOdd)
            return Math.Max(1, (int)Math.Round(size, MidpointRounding.AwayFromZero));

        return 2 * Math.Max(0, (int)Math.Round((size - 1) / 2, MidpointRounding.AwayFromZero)) + 1;
    }

    private void End()
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
