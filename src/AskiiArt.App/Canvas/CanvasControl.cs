// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Canvas.Rendering;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Tools;
using AskiiArt.Imaging.Fonts;
using AskiiArt.Imaging.Glyphs;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering;
using SkiaSharp;

namespace AskiiArt.App.Canvas;

/// <summary>
/// The one control that draws the whole canvas (ARCHITECTURE §12): only the visible cells, from
/// the composite cache and the glyph atlas, at the display's real pixel size. It also handles
/// zooming (Ctrl/Cmd + wheel), panning (wheel, middle-drag, Space + drag when the tool allows it)
/// and the hover highlight, and passes everything else to the active tool in cell terms.
/// Scrollbars bind to its offset properties, which are in device pixels.
/// </summary>
internal sealed class CanvasControl : Control, ICustomHitTest
{
    public static readonly StyledProperty<CompositeCache?> CompositeProperty =
        AvaloniaProperty.Register<CanvasControl, CompositeCache?>(nameof(Composite));

    public static readonly StyledProperty<LoadedFont?> FontProperty =
        AvaloniaProperty.Register<CanvasControl, LoadedFont?>(nameof(Font));

    public static readonly StyledProperty<GlyphAtlasCache?> AtlasesProperty =
        AvaloniaProperty.Register<CanvasControl, GlyphAtlasCache?>(nameof(Atlases));

    public static readonly StyledProperty<double> BaseFontSizeProperty =
        AvaloniaProperty.Register<CanvasControl, double>(nameof(BaseFontSize), 16);

    public static readonly StyledProperty<double> ZoomFactorProperty =
        AvaloniaProperty.Register<CanvasControl, double>(nameof(ZoomFactor), ZoomLevels.ActualSize,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsGridVisibleProperty =
        AvaloniaProperty.Register<CanvasControl, bool>(nameof(IsGridVisible));

    public static readonly StyledProperty<double> CanvasMarginProperty =
        AvaloniaProperty.Register<CanvasControl, double>(nameof(CanvasMargin), 32);

    public static readonly StyledProperty<IBrush?> PasteboardBrushProperty =
        AvaloniaProperty.Register<CanvasControl, IBrush?>(nameof(PasteboardBrush));

    public static readonly StyledProperty<IBrush?> PaperBrushProperty =
        AvaloniaProperty.Register<CanvasControl, IBrush?>(nameof(PaperBrush));

    public static readonly StyledProperty<IBrush?> InkBrushProperty =
        AvaloniaProperty.Register<CanvasControl, IBrush?>(nameof(InkBrush));

    public static readonly StyledProperty<IBrush?> GridLineBrushProperty =
        AvaloniaProperty.Register<CanvasControl, IBrush?>(nameof(GridLineBrush));

    public static readonly StyledProperty<IBrush?> HoverBrushProperty =
        AvaloniaProperty.Register<CanvasControl, IBrush?>(nameof(HoverBrush));

    public static readonly StyledProperty<IBrush?> FallbackGlyphBrushProperty =
        AvaloniaProperty.Register<CanvasControl, IBrush?>(nameof(FallbackGlyphBrush));

    public static readonly StyledProperty<IBrush?> PlaceholderGlyphBrushProperty =
        AvaloniaProperty.Register<CanvasControl, IBrush?>(nameof(PlaceholderGlyphBrush));

    public static readonly StyledProperty<IBrush?> CaretBrushProperty =
        AvaloniaProperty.Register<CanvasControl, IBrush?>(nameof(CaretBrush));

    public static readonly StyledProperty<double> CaretThicknessProperty =
        AvaloniaProperty.Register<CanvasControl, double>(nameof(CaretThickness), 2);

    public static readonly StyledProperty<ITool?> ToolProperty =
        AvaloniaProperty.Register<CanvasControl, ITool?>(nameof(Tool));

    public static readonly DirectProperty<CanvasControl, CellPoint?> HoveredCellProperty =
        AvaloniaProperty.RegisterDirect<CanvasControl, CellPoint?>(nameof(HoveredCell),
            control => control.HoveredCell, (control, value) => control.HoveredCell = value);

    public static readonly DirectProperty<CanvasControl, double> HorizontalOffsetProperty =
        AvaloniaProperty.RegisterDirect<CanvasControl, double>(nameof(HorizontalOffset),
            control => control.HorizontalOffset, (control, value) => control.ScrollTo(value, control.VerticalOffset),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly DirectProperty<CanvasControl, double> VerticalOffsetProperty =
        AvaloniaProperty.RegisterDirect<CanvasControl, double>(nameof(VerticalOffset),
            control => control.VerticalOffset, (control, value) => control.ScrollTo(control.HorizontalOffset, value),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly DirectProperty<CanvasControl, double> HorizontalMaximumProperty =
        AvaloniaProperty.RegisterDirect<CanvasControl, double>(nameof(HorizontalMaximum),
            control => control.HorizontalMaximum);

    public static readonly DirectProperty<CanvasControl, double> VerticalMaximumProperty =
        AvaloniaProperty.RegisterDirect<CanvasControl, double>(nameof(VerticalMaximum),
            control => control.VerticalMaximum);

    public static readonly DirectProperty<CanvasControl, double> HorizontalViewportProperty =
        AvaloniaProperty.RegisterDirect<CanvasControl, double>(nameof(HorizontalViewport),
            control => control.HorizontalViewport);

    public static readonly DirectProperty<CanvasControl, double> VerticalViewportProperty =
        AvaloniaProperty.RegisterDirect<CanvasControl, double>(nameof(VerticalViewport),
            control => control.VerticalViewport);

    // How far one notch of the mouse wheel scrolls, in logical pixels (the same as a ScrollViewer).
    private const double WheelScrollDistance = 50;

    private readonly CanvasSceneBuilder _sceneBuilder = new();
    private readonly WheelGestureTracker _wheelGesture = new();
    private CanvasViewport? _viewport;
    private double _renderScaling = 1;
    private Point? _pointerPosition;
    private Point? _panStart;
    private PixelPoint _panStartOffset;
    private bool _isSpaceHeld;
    private bool _isToolGestureActive;
    private Point? _zoomAnchor;
    private CompositeCache? _subscribedComposite;
    private TopLevel? _topLevel;
    private Cursor? _panCursor;
    private double _horizontalOffset;
    private double _verticalOffset;
    private bool _isPublishingViewport;

    static CanvasControl()
    {
        FocusableProperty.OverrideDefaultValue<CanvasControl>(true);
        ClipToBoundsProperty.OverrideDefaultValue<CanvasControl>(true);
        AffectsRender<CanvasControl>(IsGridVisibleProperty, PasteboardBrushProperty, PaperBrushProperty,
            InkBrushProperty, GridLineBrushProperty, HoverBrushProperty, FallbackGlyphBrushProperty,
            PlaceholderGlyphBrushProperty, CaretBrushProperty, CaretThicknessProperty);
    }

    public CompositeCache? Composite
    {
        get => GetValue(CompositeProperty);
        set => SetValue(CompositeProperty, value);
    }

    public LoadedFont? Font
    {
        get => GetValue(FontProperty);
        set => SetValue(FontProperty, value);
    }

    /// <summary>The shared glyph atlases; the control picks the one for its font and zoom.</summary>
    public GlyphAtlasCache? Atlases
    {
        get => GetValue(AtlasesProperty);
        set => SetValue(AtlasesProperty, value);
    }

    /// <summary>The font size at 100% zoom, in logical pixels.</summary>
    public double BaseFontSize
    {
        get => GetValue(BaseFontSizeProperty);
        set => SetValue(BaseFontSizeProperty, value);
    }

    public double ZoomFactor
    {
        get => GetValue(ZoomFactorProperty);
        set => SetValue(ZoomFactorProperty, value);
    }

    public bool IsGridVisible
    {
        get => GetValue(IsGridVisibleProperty);
        set => SetValue(IsGridVisibleProperty, value);
    }

    /// <summary>The empty space around the canvas when scrolled to an edge, in logical pixels.</summary>
    public double CanvasMargin
    {
        get => GetValue(CanvasMarginProperty);
        set => SetValue(CanvasMarginProperty, value);
    }

    public IBrush? PasteboardBrush
    {
        get => GetValue(PasteboardBrushProperty);
        set => SetValue(PasteboardBrushProperty, value);
    }

    public IBrush? PaperBrush
    {
        get => GetValue(PaperBrushProperty);
        set => SetValue(PaperBrushProperty, value);
    }

    public IBrush? InkBrush
    {
        get => GetValue(InkBrushProperty);
        set => SetValue(InkBrushProperty, value);
    }

    public IBrush? GridLineBrush
    {
        get => GetValue(GridLineBrushProperty);
        set => SetValue(GridLineBrushProperty, value);
    }

    public IBrush? HoverBrush
    {
        get => GetValue(HoverBrushProperty);
        set => SetValue(HoverBrushProperty, value);
    }

    public IBrush? FallbackGlyphBrush
    {
        get => GetValue(FallbackGlyphBrushProperty);
        set => SetValue(FallbackGlyphBrushProperty, value);
    }

    public IBrush? PlaceholderGlyphBrush
    {
        get => GetValue(PlaceholderGlyphBrushProperty);
        set => SetValue(PlaceholderGlyphBrushProperty, value);
    }

    public IBrush? CaretBrush
    {
        get => GetValue(CaretBrushProperty);
        set => SetValue(CaretBrushProperty, value);
    }

    /// <summary>The text caret outline's thickness, in logical pixels.</summary>
    public double CaretThickness
    {
        get => GetValue(CaretThicknessProperty);
        set => SetValue(CaretThicknessProperty, value);
    }

    /// <summary>The active tool, which gets the pointer and keyboard input the canvas doesn't use itself.</summary>
    public ITool? Tool
    {
        get => GetValue(ToolProperty);
        set => SetValue(ToolProperty, value);
    }

    /// <summary>The cell under the pointer, or null when the pointer isn't over the canvas.</summary>
    public CellPoint? HoveredCell
    {
        get;
        set => SetAndRaise(HoveredCellProperty, ref field, value);
    }

    public double HorizontalOffset
    {
        get => _horizontalOffset;
        set => ScrollTo(value, VerticalOffset);
    }

    public double VerticalOffset
    {
        get => _verticalOffset;
        set => ScrollTo(HorizontalOffset, value);
    }

    public double HorizontalMaximum
    {
        get;
        private set => SetAndRaise(HorizontalMaximumProperty, ref field, value);
    }

    public double VerticalMaximum
    {
        get;
        private set => SetAndRaise(VerticalMaximumProperty, ref field, value);
    }

    public double HorizontalViewport
    {
        get;
        private set => SetAndRaise(HorizontalViewportProperty, ref field, value);
    }

    public double VerticalViewport
    {
        get;
        private set => SetAndRaise(VerticalViewportProperty, ref field, value);
    }

    /// <summary>Where the canvas currently sits; null until the control has a size and a document.</summary>
    internal CanvasViewport? Viewport => _viewport;

    /// <summary>Which keys go to the tool, and as what. Tests swap in another platform's keys.</summary>
    internal ToolKeymap Keymap { get; set; } = ToolKeymap.ForCurrentPlatform();

    // The whole control takes pointer input, including the pasteboard, before anything is drawn.
    public bool HitTest(Point point) => new Rect(Bounds.Size).Contains(point);

    public override void Render(DrawingContext context)
    {
        if (_viewport is null || Composite is null || GetAtlas() is not { } atlas)
            return;

        var caretThickness = Math.Max(1, (int)Math.Round(CaretThickness * _renderScaling));
        var overlays = new CanvasOverlays(IsGridVisible, HoveredCell, Tool?.Overlay.Caret, caretThickness);
        var scene = _sceneBuilder.Build(Composite, atlas, _viewport, GetColors(), overlays);
        context.Custom(new CanvasDrawOperation(new Rect(Bounds.Size), scene));
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == CompositeProperty)
            FollowComposite(change.GetNewValue<CompositeCache?>());

        if (change.Property == ToolProperty)
            FollowTool(change.GetOldValue<ITool?>(), change.GetNewValue<ITool?>());

        if (change.Property == ZoomFactorProperty)
        {
            // A wheel zoom anchors on the pointer; any other zoom (a shortcut, say) on the centre.
            UpdateViewport(_zoomAnchor ?? ViewportCentre());
            _zoomAnchor = null;
        }
        else if (change.Property == CompositeProperty || change.Property == FontProperty ||
                 change.Property == AtlasesProperty || change.Property == BaseFontSizeProperty ||
                 change.Property == CanvasMarginProperty)
        {
            UpdateViewport();
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateViewport();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _topLevel = TopLevel.GetTopLevel(this);
        if (_topLevel is not null)
            _topLevel.ScalingChanged += OnScalingChanged;

        UpdateViewport();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_topLevel is not null)
            _topLevel.ScalingChanged -= OnScalingChanged;

        _topLevel = null;
        FollowComposite(null);
        FollowTool(Tool, null);
        _sceneBuilder.ReleaseCachedPages();
        _panCursor?.Dispose();
        _panCursor = null;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        _pointerPosition = ToDevicePixels(e.GetPosition(this));

        if (_panStart is { } start && _viewport is not null)
        {
            var current = _pointerPosition.Value;
            var offset = new PixelPoint(
                _panStartOffset.X - (int)Math.Round(current.X - start.X),
                _panStartOffset.Y - (int)Math.Round(current.Y - start.Y));
            SetViewport(_viewport.ScrolledTo(offset));
        }
        else if (Tool is { } tool && ToToolInput(_pointerPosition.Value, e.KeyModifiers) is { } input)
        {
            tool.PointerMoved(input);
        }

        UpdateHoveredCell();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _pointerPosition = null;
        UpdateHoveredCell();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();

        var point = e.GetCurrentPoint(this);
        if (_viewport is null)
            return;

        var startsPan = point.Properties.IsMiddleButtonPressed ||
                        (_isSpaceHeld && point.Properties.IsLeftButtonPressed);
        if (startsPan)
        {
            _panStart = ToDevicePixels(point.Position);
            _panStartOffset = _viewport.ScrollOffset;
            e.Pointer.Capture(this);
            UpdateCursor();
            e.Handled = true;
        }
        else if (point.Properties.IsLeftButtonPressed && Tool is { } tool &&
                 ToToolInput(ToDevicePixels(point.Position), e.KeyModifiers) is { } input)
        {
            _isToolGestureActive = true;
            e.Pointer.Capture(this);
            tool.PointerPressed(input);
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_panStart is not null)
        {
            EndPan(e.Pointer);
            e.Handled = true;
        }
        else if (_isToolGestureActive)
        {
            _isToolGestureActive = false;
            e.Pointer.Capture(null);
            if (Tool is { } tool && ToToolInput(ToDevicePixels(e.GetPosition(this)), e.KeyModifiers) is { } input)
                tool.PointerReleased(input);

            e.Handled = true;
        }
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _panStart = null;
        _isToolGestureActive = false;
        UpdateCursor();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (_viewport is null)
            return;

        var action = _wheelGesture.Track(e.Timestamp, e.KeyModifiers.HasFlag(PlatformCommandModifiers()),
            e.KeyModifiers.HasFlag(KeyModifiers.Shift));
        switch (action)
        {
            case WheelAction.Zoom:
                ZoomByWheel(e);
                break;
            case WheelAction.Scroll:
                ScrollByWheel(e.Delta, _viewport);
                break;
            case WheelAction.ScrollSideways:
                // Shift turns a vertical wheel into a horizontal one, as in most apps. macOS does
                // that itself, so a delta that's already sideways is left alone.
                ScrollByWheel(e.Delta.X == 0 ? new Vector(e.Delta.Y, 0) : e.Delta, _viewport);
                break;
        }

        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        // A text tool wants Space as a typed space, so it isn't handled here: on macOS a handled
        // key never reaches text input.
        if (e.Key == Key.Space && (Tool?.AllowsSpacePanning ?? true))
        {
            _isSpaceHeld = true;
            UpdateCursor();
            e.Handled = true;
        }
        else if (FindToolKey(e) is { } key && Tool is { } tool && tool.KeyPressed(key, ToToolModifiers(e.KeyModifiers)))
        {
            e.Handled = true;
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.Key != Key.Space || !_isSpaceHeld)
            return;

        _isSpaceHeld = false;
        UpdateCursor();
        e.Handled = true;
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        if (e.Text is { Length: > 0 } text && Tool is { } tool && tool.TextEntered(text))
            e.Handled = true;
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        _isSpaceHeld = false;
        UpdateCursor();
    }

    private static SKColor ToSkColor(IBrush? brush)
    {
        if (brush is not ISolidColorBrush solid)
            return SKColors.Transparent;

        var color = solid.Color;
        var alpha = (byte)Math.Round(color.A * Math.Clamp(solid.Opacity, 0, 1));
        return new SKColor(color.R, color.G, color.B, alpha);
    }

    private void ZoomByWheel(PointerWheelEventArgs e)
    {
        var steps = _wheelGesture.TakeZoomSteps(e.Delta.Y);
        if (steps != 0)
            ZoomAt(ToDevicePixels(e.GetPosition(this)), steps);
    }

    private void ScrollByWheel(Vector delta, CanvasViewport viewport)
    {
        var distance = WheelScrollDistance * _renderScaling;
        SetViewport(viewport.ScrolledBy(-(int)Math.Round(delta.X * distance), -(int)Math.Round(delta.Y * distance)));
    }

    // Zooms in by positive steps and out by negative ones.
    private void ZoomAt(Point anchor, int steps)
    {
        var factor = ZoomFactor;
        for (var step = 0; step < Math.Abs(steps); step++)
            factor = steps > 0 ? ZoomLevels.StepIn(factor) : ZoomLevels.StepOut(factor);

        _zoomAnchor = anchor;
        SetCurrentValue(ZoomFactorProperty, factor);
        _zoomAnchor = null;
    }

    private void EndPan(IPointer pointer)
    {
        _panStart = null;
        pointer.Capture(null);
        UpdateCursor();
    }

    private void ScrollTo(double horizontal, double vertical)
    {
        if (_viewport is null || _isPublishingViewport)
            return;

        SetViewport(_viewport.ScrolledTo(new PixelPoint((int)Math.Round(horizontal), (int)Math.Round(vertical))));
    }

    /// <summary>
    /// Rebuilds the viewport after anything that changes the cell size, viewport size or canvas
    /// size. On a zoom, the canvas spot under <paramref name="zoomAnchor"/> stays put.
    /// </summary>
    private void UpdateViewport(Point? zoomAnchor = null)
    {
        _renderScaling = _topLevel?.RenderScaling ?? 1;
        if (Composite is null || GetAtlas() is not { } atlas)
        {
            SetViewport(null);
            return;
        }

        var cellSize = new PixelSize(atlas.Metrics.Width, atlas.Metrics.Height);
        var viewportSize = PixelSize.FromSize(Bounds.Size, _renderScaling);
        var margin = (int)Math.Round(CanvasMargin * _renderScaling);

        if (_viewport is null)
        {
            SetViewport(new CanvasViewport(Composite.Size, cellSize, viewportSize, margin, default));
            return;
        }

        var resized = _viewport.WithCanvasSize(Composite.Size).WithViewportSize(viewportSize).WithMargin(margin);
        SetViewport(cellSize == resized.CellSize ? resized : resized.Zoomed(cellSize, zoomAnchor ?? ViewportCentre()));
    }

    private void SetViewport(CanvasViewport? viewport)
    {
        _viewport = viewport;

        // A bound scrollbar clamps its old value as soon as its maximum shrinks, and writes the
        // clamped value back through the two-way binding before the new offset arrives. Those
        // writes are echoes of the old offset, so they're ignored here.
        _isPublishingViewport = true;
        try
        {
            HorizontalMaximum = viewport?.MaximumScrollOffset.X ?? 0;
            VerticalMaximum = viewport?.MaximumScrollOffset.Y ?? 0;
            HorizontalViewport = viewport?.ViewportSize.Width ?? 0;
            VerticalViewport = viewport?.ViewportSize.Height ?? 0;
            SetAndRaise(HorizontalOffsetProperty, ref _horizontalOffset, viewport?.ScrollOffset.X ?? 0);
            SetAndRaise(VerticalOffsetProperty, ref _verticalOffset, viewport?.ScrollOffset.Y ?? 0);
        }
        finally
        {
            _isPublishingViewport = false;
        }

        UpdateHoveredCell();
        InvalidateVisual();
    }

    private void UpdateHoveredCell()
    {
        var hovered = _pointerPosition is { } position ? _viewport?.CellAt(position) : null;
        if (hovered == HoveredCell)
            return;

        HoveredCell = hovered;
        InvalidateVisual();
    }

    private void UpdateCursor()
    {
        if (_panStart is not null || _isSpaceHeld)
            Cursor = _panCursor ??= new Cursor(StandardCursorType.Hand);
        else
            Cursor = null;
    }

    private GlyphAtlas? GetAtlas()
    {
        if (Font is null || Atlases is null)
            return null;

        var pixelSize = BaseFontSize * ZoomFactor * _renderScaling;
        var clamped = Math.Clamp((float)pixelSize, CellMetrics.MinimumPixelSize, CellMetrics.MaximumPixelSize);
        return Atlases.GetAtlas(Font, clamped, GlyphRenderOptions.Default);
    }

    private CanvasColors GetColors() => new(
        ToSkColor(PasteboardBrush),
        ToSkColor(PaperBrush),
        ToSkColor(InkBrush),
        ToSkColor(GridLineBrush),
        ToSkColor(HoverBrush),
        ToSkColor(FallbackGlyphBrush),
        ToSkColor(PlaceholderGlyphBrush),
        ToSkColor(CaretBrush));

    private void FollowTool(ITool? oldTool, ITool? newTool)
    {
        if (oldTool is not null)
            oldTool.OverlayChanged -= OnToolOverlayChanged;

        if (newTool is not null)
            newTool.OverlayChanged += OnToolOverlayChanged;

        InvalidateVisual();
    }

    private void OnToolOverlayChanged(object? sender, EventArgs e)
    {
        ScrollCaretIntoView();
        InvalidateVisual();
    }

    // Typing or arrow keys can move the caret off screen; scroll just far enough to show it.
    private void ScrollCaretIntoView()
    {
        if (_viewport is null || Tool?.Overlay.Caret is not { } caret || !_viewport.CanvasSize.Contains(caret))
            return;

        var cell = _viewport.GetCellBounds(caret);
        var size = _viewport.ViewportSize;
        var deltaX = DistanceToShow(cell.X, cell.Right, size.Width);
        var deltaY = DistanceToShow(cell.Y, cell.Bottom, size.Height);
        if (deltaX != 0 || deltaY != 0)
            SetViewport(_viewport.ScrolledBy(deltaX, deltaY));
    }

    // How far to scroll so the span from start to end is on screen, preferring its start.
    private static int DistanceToShow(int start, int end, int viewportLength)
    {
        if (start < 0)
            return start;

        if (end > viewportLength)
            return Math.Min(end - viewportLength, start);

        return 0;
    }

    private ToolPointerInput? ToToolInput(Point devicePoint, KeyModifiers modifiers)
    {
        if (_viewport is null)
            return null;

        var origin = _viewport.CanvasOrigin;
        var column = (devicePoint.X - origin.X) / _viewport.CellSize.Width;
        var row = (devicePoint.Y - origin.Y) / _viewport.CellSize.Height;
        var cell = new CellPoint((int)Math.Floor(column), (int)Math.Floor(row));
        return new ToolPointerInput(cell, column - cell.X, row - cell.Y, ToToolModifiers(modifiers));
    }

    // A modifier tools don't know (Ctrl on a Mac, the Windows key) makes it a different shortcut.
    private ToolKey? FindToolKey(KeyEventArgs e)
    {
        var known = PlatformCommandModifiers() | KeyModifiers.Shift | KeyModifiers.Alt;
        if ((e.KeyModifiers & ~known) != 0)
            return null;

        return Keymap.Find(e.Key, ToToolModifiers(e.KeyModifiers));
    }

    private static ToolModifiers ToToolModifiers(KeyModifiers modifiers)
    {
        var toolModifiers = ToolModifiers.None;
        if ((modifiers & PlatformCommandModifiers()) != 0)
            toolModifiers |= ToolModifiers.Command;

        if (modifiers.HasFlag(KeyModifiers.Shift))
            toolModifiers |= ToolModifiers.Shift;

        if (modifiers.HasFlag(KeyModifiers.Alt))
            toolModifiers |= ToolModifiers.Alt;

        return toolModifiers;
    }

    private void FollowComposite(CompositeCache? composite)
    {
        if (_subscribedComposite is not null)
            _subscribedComposite.Changed -= OnCompositeChanged;

        _subscribedComposite = composite;
        if (composite is not null)
            composite.Changed += OnCompositeChanged;
    }

    private void OnCompositeChanged(object? sender, EventArgs e)
    {
        if (_viewport is not null && Composite is not null && _viewport.CanvasSize != Composite.Size)
            UpdateViewport();

        InvalidateVisual();
    }

    private void OnScalingChanged(object? sender, EventArgs e) => UpdateViewport();

    private Point ViewportCentre() => _viewport is null
        ? default
        : new Point(_viewport.ViewportSize.Width / 2.0, _viewport.ViewportSize.Height / 2.0);

    private Point ToDevicePixels(Point logical) => logical * _renderScaling;

    // Ctrl on Windows and Linux, Cmd on macOS.
    private static KeyModifiers PlatformCommandModifiers() =>
        Application.Current?.PlatformSettings?.HotkeyConfiguration.CommandModifiers ?? KeyModifiers.Control;
}
