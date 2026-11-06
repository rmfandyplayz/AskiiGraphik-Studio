// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Solving;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace AskiiArt.App.Shell.Docking;

/// <summary>
/// A strip in a gap between widgets or along a side's inner edge, which resizes the windows when it's dragged. It paints
/// itself see-through all over, so that it catches the pointer instead of letting it through to the canvas. A line along its middle shows
/// faintly while the pointer is over it, and solidly while it's held. Placeholder: the owner designs the real one.
/// </summary>
internal sealed class StackResizeHandleView : Control
{
    public static readonly StyledProperty<IBrush?> HoverLineProperty =
        AvaloniaProperty.Register<StackResizeHandleView, IBrush?>(nameof(HoverLine));

    public static readonly StyledProperty<IBrush?> HeldLineProperty =
        AvaloniaProperty.Register<StackResizeHandleView, IBrush?>(nameof(HeldLine));

    public static readonly StyledProperty<double> LineThicknessProperty =
        AvaloniaProperty.Register<StackResizeHandleView, double>(nameof(LineThickness));

    private bool _isHeld;

    static StackResizeHandleView() => AffectsRender<StackResizeHandleView>(IsPointerOverProperty, HoverLineProperty,
        HeldLineProperty, LineThicknessProperty);

    /// <param name="id">Which handle it is.</param>
    public StackResizeHandleView(StackResizeHandleId id)
    {
        Id = id;
        Cursor = new Cursor(IsAcross ? StandardCursorType.SizeNorthSouth : StandardCursorType.SizeWestEast);
        Bind(HoverLineProperty, this.GetResourceObservable("AppResizeLineHoverBrush"));
        Bind(HeldLineProperty, this.GetResourceObservable("AppResizeLineHeldBrush"));
        Bind(LineThicknessProperty, this.GetResourceObservable("AppResizeLineThickness"));

        // Including handled presses, because the resize takes them.
        AddHandler(PointerPressedEvent, (_, e) => SetHeld(e.GetCurrentPoint(this).Properties.IsLeftButtonPressed),
            handledEventsToo: true);
        AddHandler(PointerReleasedEvent, (_, _) => SetHeld(false), handledEventsToo: true);
        AddHandler(PointerCaptureLostEvent, (_, _) => SetHeld(false), handledEventsToo: true);
    }

    /// <summary>Which handle it is.</summary>
    public StackResizeHandleId Id { get; }

    /// <summary>Whether its line runs across the side, as in a gap between widgets, rather than along it.</summary>
    public bool IsAcross => Id.Gap is not null;

    /// <summary>Whether it's held down, which shows its line solidly.</summary>
    public bool IsHeld => _isHeld;

    public IBrush? HoverLine
    {
        get => GetValue(HoverLineProperty);
        set => SetValue(HoverLineProperty, value);
    }

    public IBrush? HeldLine
    {
        get => GetValue(HeldLineProperty);
        set => SetValue(HeldLineProperty, value);
    }

    public double LineThickness
    {
        get => GetValue(LineThicknessProperty);
        set => SetValue(LineThicknessProperty, value);
    }

    /// <summary>The line's brush right now, or null when it doesn't show.</summary>
    public IBrush? LineBrush => _isHeld ? HeldLine : IsPointerOver ? HoverLine : null;

    public override void Render(DrawingContext context)
    {
        var size = Bounds.Size;
        context.FillRectangle(Brushes.Transparent, new Rect(size));

        if (LineBrush is not { } brush)
            return;

        var line = IsAcross
            ? new Rect(0, (size.Height - LineThickness) / 2, size.Width, LineThickness)
            : new Rect((size.Width - LineThickness) / 2, 0, LineThickness, size.Height);
        context.FillRectangle(brush, line);
    }

    private void SetHeld(bool isHeld)
    {
        if (_isHeld == isHeld)
            return;

        _isHeld = isHeld;
        InvalidateVisual();
    }
}
