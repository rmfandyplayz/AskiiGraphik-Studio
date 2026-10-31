// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Canvas.Rendering;
using AskiiArt.Core.Documents;
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
/// zooming (Ctrl/Cmd + wheel), panning (wheel, middle-drag, Space + drag) and the hover highlight.
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
        AvaloniaProperty.RegisterDirect<CanvasControl, double>(nameof(HorizontalMaximum), control => control.HorizontalMaximum);

    public static readonly DirectProperty<CanvasControl, double> VerticalMaximumProperty =
        AvaloniaProperty.RegisterDirect<CanvasControl, double>(nameof(VerticalMaximum), control => control.VerticalMaximum);

    public static readonly DirectProperty<CanvasControl, double> HorizontalViewportProperty =
        AvaloniaProperty.RegisterDirect<CanvasControl, double>(nameof(HorizontalViewport), control => control.HorizontalViewport);

    public static readonly DirectProperty<CanvasControl, double> VerticalViewportProperty =
        AvaloniaProperty.RegisterDirect<CanvasControl, double>(nameof(VerticalViewport), control => control.VerticalViewport);

    // How far one notch of the mouse wheel scrolls, in logical pixels (the same as a ScrollViewer).
    private const double WheelScrollDistance = 50;

    private readonly CanvasSceneBuilder _sceneBuilder = new();
    private CanvasViewport? _viewport;
    private double _renderScaling = 1;
    private Point? _pointerPosition;
    private Point? _panStart;
    private PixelPoint _panStartOffset;
    private bool _isSpaceHeld;
    private Point? _zoomAnchor;
    private CompositeCache? _subscribedComposite;
    private TopLevel? _topLevel;
    private Cursor? _panCursor;
    private double _horizontalOffset;
    private double _verticalOffset;

    static CanvasControl()
    {
        FocusableProperty.OverrideDefaultValue<CanvasControl>(true);
        ClipToBoundsProperty.OverrideDefaultValue<CanvasControl>(true);
        AffectsRender<CanvasControl>(IsGridVisibleProperty, PasteboardBrushProperty, PaperBrushProperty,
            InkBrushProperty, GridLineBrushProperty, HoverBrushProperty, FallbackGlyphBrushProperty,
            PlaceholderGlyphBrushProperty);
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

    // The whole control takes pointer input, including the pasteboard, before anything is drawn.
    public bool HitTest(Point point) => new Rect(Bounds.Size).Contains(point);

    public override void Render(DrawingContext context)
    {
        if (_viewport is null || Composite is null || GetAtlas() is not { } atlas)
            return;

        var scene = _sceneBuilder.Build(Composite, atlas, _viewport, GetColors(), IsGridVisible, HoveredCell);
        context.Custom(new CanvasDrawOperation(new Rect(Bounds.Size), scene));
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == CompositeProperty)
            FollowComposite(change.GetNewValue<CompositeCache?>());

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
        var startsPan = point.Properties.IsMiddleButtonPressed ||
                        (_isSpaceHeld && point.Properties.IsLeftButtonPressed);
        if (!startsPan || _viewport is null)
            return;

        _panStart = ToDevicePixels(point.Position);
        _panStartOffset = _viewport.ScrollOffset;
        e.Pointer.Capture(this);
        UpdateCursor();
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_panStart is null)
            return;

        EndPan(e.Pointer);
        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _panStart = null;
        UpdateCursor();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (_viewport is null)
            return;

        if (e.KeyModifiers.HasFlag(PlatformCommandModifiers()))
            ZoomByWheel(e);
        else
            ScrollByWheel(e, _viewport);

        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key != Key.Space)
            return;

        _isSpaceHeld = true;
        UpdateCursor();
        e.Handled = true;
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.Key != Key.Space)
            return;

        _isSpaceHeld = false;
        UpdateCursor();
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
        if (e.Delta.Y != 0)
            ZoomAt(ToDevicePixels(e.GetPosition(this)), zoomIn: e.Delta.Y > 0);
    }

    private void ScrollByWheel(PointerWheelEventArgs e, CanvasViewport viewport)
    {
        // Shift turns a vertical wheel into a horizontal one, as in most apps; touchpads also
        // scroll sideways on their own.
        var delta = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? new Vector(e.Delta.Y, e.Delta.X) : e.Delta;
        var distance = WheelScrollDistance * _renderScaling;
        SetViewport(viewport.ScrolledBy(-(int)Math.Round(delta.X * distance), -(int)Math.Round(delta.Y * distance)));
    }

    private void ZoomAt(Point anchor, bool zoomIn)
    {
        var factor = zoomIn ? ZoomLevels.StepIn(ZoomFactor) : ZoomLevels.StepOut(ZoomFactor);
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
        if (_viewport is null)
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
        HorizontalMaximum = viewport?.MaximumScrollOffset.X ?? 0;
        VerticalMaximum = viewport?.MaximumScrollOffset.Y ?? 0;
        HorizontalViewport = viewport?.ViewportSize.Width ?? 0;
        VerticalViewport = viewport?.ViewportSize.Height ?? 0;
        SetAndRaise(HorizontalOffsetProperty, ref _horizontalOffset, viewport?.ScrollOffset.X ?? 0);
        SetAndRaise(VerticalOffsetProperty, ref _verticalOffset, viewport?.ScrollOffset.Y ?? 0);
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
        ToSkColor(PlaceholderGlyphBrush));

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
