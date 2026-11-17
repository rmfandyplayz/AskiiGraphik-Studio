// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Compositing;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Ramps;
using AskiiArt.Core.Tools.Footprints;
using AskiiArt.Core.Tools.Options;

namespace AskiiArt.Core.Tools.Brush;

/// <summary>
/// The Brush, which paints a ramp (ADR 0049). Each cell under the tip moves from its darkness now towards the brush's
/// intensity, by the tip's weight there, and shows the ramp's glyph for where it gets to. A cell's darkness is its
/// level in the ramp (<see cref="Ramp.NearestLevel"/>), and an empty cell's is 0. Within one stroke a cell moves by the
/// greatest weight that reached it, from where it was before the stroke, so going over it again adds nothing, and the
/// next stroke takes it further. A cell whose level changes gets the current colours, as typing gives them, and one
/// lightened to nothing loses its glyph and keeps its background. It doesn't paint while the ramp has characters the
/// document's character set can't hold. The tip is the Brush group's (ADR 0047), and [ and ] change its size. The
/// moves of one stroke coalesce into one undo step. Alt+click (Option+click) picks both colours up from what the canvas
/// shows instead.
/// </summary>
public sealed class BrushTool : ITool
{
    /// <summary>The tool's id.</summary>
    public const string Id = "askiiart.brush";

    /// <summary>The brush's identity. The name key is the App's resource key.</summary>
    public static ToolDescriptor ToolDescriptor { get; } = new(Id, "ToolBrush", "tool.brush", "paint");

    /// <summary>The Brush group's tip: its size, shape and proportions, which the FG and BG Brushes share.</summary>
    public static ToolOptionGroup TipOptions { get; } = BrushTipOptions.CreateGroup(Id);

    /// <summary>Registers the brush, as DI does.</summary>
    public static ToolDefinition Definition { get; } = new(ToolDescriptor, context => new BrushTool(context.Session,
        context.Colors, new BrushTipSource(context.Settings.For(TipOptions), context.CellWidthToHeight),
        context.Settings.For(RampBrushOptions.Group), context.Densities))
    {
        OptionGroups = [TipOptions, RampBrushOptions.Group],
        WarningFor = RampBrushOptions.WarningFor
    };

    private readonly EditingSession _session;
    private readonly PaintColors _colors;
    private readonly BrushTipSource _tip;
    private readonly ToolOptionValues _options;
    private readonly Func<IGlyphDensityProvider> _densities;
    private ToolOverlay _overlay = ToolOverlay.None;
    private Gesture _gesture;

    // The cell a pick last read.
    private CellPoint _lastPickedCell;

    // The colours a pick replaces, which Esc gives back.
    private (CellColor Foreground, CellColor Background) _colorsBeforePick;

    private Stroke? _stroke;

    /// <summary>
    /// Creates a brush for <paramref name="session"/> that paints with <paramref name="tip"/>'s footprint, the ramp
    /// and strength <paramref name="options"/> say, and <paramref name="colors"/>. Call <see cref="Deactivate"/> when
    /// done with it, so it stops following the tip's settings.
    /// </summary>
    /// <param name="session">The editing session the brush changes.</param>
    /// <param name="colors">The current colours, which painted cells get.</param>
    /// <param name="tip">The Brush group's tip.</param>
    /// <param name="options">The values of <see cref="RampBrushOptions.Group"/>.</param>
    /// <param name="densities">How dark glyphs look in the document's font, now.</param>
    public BrushTool(EditingSession session, PaintColors colors, BrushTipSource tip, ToolOptionValues options,
        Func<IGlyphDensityProvider> densities)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(colors);
        ArgumentNullException.ThrowIfNull(tip);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(densities);
        if (options.Group != RampBrushOptions.Group)
            throw new ArgumentException("These aren't the Brush's own options.", nameof(options));

        _session = session;
        _colors = colors;
        _tip = tip;
        _options = options;
        _densities = densities;
        _tip.Values.Changed += OnTipChanged;
    }

    private enum Gesture
    {
        None,
        Painting,
        Picking
    }

    /// <summary>Raised when the tip's size or shape changes, so the canvas outlines the new one.</summary>
    public event EventHandler? OverlayChanged;

    /// <inheritdoc/>
    public ToolDescriptor Descriptor => ToolDescriptor;

    /// <summary>True: Space + drag pans.</summary>
    public bool AllowsSpacePanning => true;

    /// <summary>False: dragging paints.</summary>
    public bool PansWithPrimaryButton => false;

    /// <summary>False: the brush takes no text, apart from its keys.</summary>
    public bool IsTakingText => false;

    /// <summary>The tip's footprint, for the canvas to outline under the pointer.</summary>
    public ToolOverlay Overlay
    {
        get
        {
            var footprint = _stroke?.Falloff.Footprint ?? _tip.Footprint;
            if (_overlay.Footprint != footprint)
                _overlay = new ToolOverlay(Caret: null, footprint);

            return _overlay;
        }
    }

    /// <summary>
    /// With Alt, picks both colours up from the cell. Otherwise starts a stroke, unless the active layer is locked or
    /// hidden, or the document's character set can't hold the ramp. A stroke may start outside the canvas.
    /// </summary>
    public void PointerPressed(ToolPointerInput input)
    {
        if (input.Modifiers.HasFlag(ToolModifiers.Alt))
        {
            StartPicking(input.Cell);
            return;
        }

        var document = _session.Document;
        if (!_session.CanEditActiveLayer || !RampBrushOptions.CanPaintIn(_options, document))
            return;

        var softness = _options.Get(RampBrushOptions.Softness) / 100.0;
        _gesture = Gesture.Painting;
        _stroke = new Stroke(
            RampBrushOptions.ReadRamp(_options).Ramp,
            _options.Get(RampBrushOptions.Intensity) / 100.0,
            BrushFalloff.Create(_tip.Footprint, softness),
            _colors.ForegroundIn(document.ColorMode),
            _colors.BackgroundIn(document.ColorMode));
        _stroke.LastTopLeft = _stroke.Falloff.Footprint.TopLeftAt(input);
        Paint(_stroke.LastTopLeft, _stroke.LastTopLeft);
    }

    /// <summary>
    /// Continues the stroke in a straight line, because a fast drag reports cells far apart. While picking, picks
    /// from each cell the pointer reaches.
    /// </summary>
    public void PointerMoved(ToolPointerInput input)
    {
        if (_gesture == Gesture.Picking && input.Cell != _lastPickedCell)
        {
            Pick(input.Cell);
        }
        else if (_gesture == Gesture.Painting && _stroke is not null)
        {
            var topLeft = _stroke.Falloff.Footprint.TopLeftAt(input);
            if (topLeft == _stroke.LastTopLeft)
                return;

            Paint(_stroke.LastTopLeft, topLeft);
            _stroke.LastTopLeft = topLeft;
        }
    }

    /// <inheritdoc/>
    public void PointerReleased(ToolPointerInput input) => EndGesture();

    /// <summary>
    /// Esc during a stroke takes it back as if it never happened, and Esc while picking gives back the colours from
    /// before. Otherwise Esc isn't the brush's, so the App switches to the Hand tool (ADR 0041).
    /// </summary>
    public bool KeyPressed(ToolKey key, ToolModifiers modifiers)
    {
        if (key != ToolKey.Escape || _gesture == Gesture.None)
            return false;

        if (_gesture == Gesture.Painting && _stroke?.UndoCountAfterLastEdit == _session.History.UndoCount)
            _session.History.CancelNewestStep();
        else if (_gesture == Gesture.Picking)
            _colors.Set(_colorsBeforePick.Foreground, _colorsBeforePick.Background);

        EndGesture();
        return true;
    }

    /// <summary>
    /// [ makes the tip a cell smaller and ] a cell bigger. They're typed text rather than keys, so they work with any
    /// keyboard layout that types them.
    /// </summary>
    public bool TextEntered(string text) => BrushTipOptions.TryUseSizeKey(_tip.Values, text);

    /// <summary>Keeps what the stroke has painted or the colours picked so far, and stops following the tip.</summary>
    public void Deactivate()
    {
        EndGesture();
        _tip.Values.Changed -= OnTipChanged;
    }

    private void OnTipChanged(object? sender, EventArgs e) => OverlayChanged?.Invoke(this, EventArgs.Empty);

    private void EndGesture()
    {
        var hadStroke = _stroke is not null;
        _gesture = Gesture.None;
        _stroke = null;

        // A size changed during the stroke shows only now.
        if (hadStroke && _overlay.Footprint != _tip.Footprint)
            OverlayChanged?.Invoke(this, EventArgs.Empty);
    }

    // Picking works on any layer, locked or hidden, because it takes what the canvas shows. A monochrome document
    // shows no colours, and picking there would only lose the ones chosen, so it does nothing.
    private void StartPicking(CellPoint point)
    {
        if (_session.Document.ColorMode == ColorMode.Monochrome)
            return;

        _gesture = Gesture.Picking;
        _colorsBeforePick = (_colors.Foreground, _colors.Background);
        Pick(point);
    }

    private void Pick(CellPoint point)
    {
        _lastPickedCell = point;
        var document = _session.Document;
        if (!document.CanvasSize.Contains(point))
            return;

        // A cell with no glyph shows no text colour, so it gives none, as does one in the canvas's own ink.
        var shown = Compositor.CompositeCell(document.ActiveFrame, point);
        _colors.Set(shown.Foreground, shown.Background);
    }

    private void Paint(CellPoint fromTopLeft, CellPoint toTopLeft)
    {
        // Undoing during a stroke can take away the layer it started on, or lock it.
        if (!_session.CanEditActiveLayer || _stroke is null)
            return;

        var document = _session.Document;
        var layer = _session.ActiveLayer;
        var cells = new CellEditBuilder(document, layer.Id);
        var reached = FootprintSweep.CoverWeighted(_stroke.Falloff, fromTopLeft, toTopLeft, document.CanvasSize);
        foreach (var (point, weight) in reached)
        {
            var now = layer.GetCell(point);
            if (!_stroke.Reached.TryGetValue(point, out var before))
                before = new ReachedCell(now, 0);

            if (weight <= before.Weight)
                continue;

            _stroke.Reached[point] = before with { Weight = weight };
            var painted = Blend(before.Cell, weight);
            if (painted != now)
                cells.Set(point, painted);
        }

        if (cells.Build(UndoStepNames.Paint) is not { } command)
            return;

        _session.History.Execute(command, _stroke.Undo);
        _stroke.UndoCountAfterLastEdit = _session.History.UndoCount;
    }

    // The cell as it was before the stroke, moved towards the brush's intensity by weight.
    private Cell Blend(Cell before, double weight)
    {
        var stroke = _stroke!;
        var ramp = stroke.Ramp;
        var levelBefore = LevelOf(before);
        var darknessBefore = ramp.IntensityOf(levelBefore);
        var level = ramp.LevelAt(darknessBefore + (stroke.Intensity - darknessBefore) * weight);

        // A glyph the ramp doesn't hold stays as it is until the brush moves it a whole level.
        if (level == levelBefore)
            return before;

        if (ramp.GlyphOf(level) is not { } glyph)
            return before.WithoutGlyph();

        var background = stroke.Background.IsUnset ? before.Background : stroke.Background;
        return Cell.FromGlyph(glyph, stroke.Foreground, background);
    }

    private int LevelOf(Cell cell)
    {
        if (!cell.HasGlyph)
            return 0;

        var stroke = _stroke!;
        if (!stroke.Levels.TryGetValue(cell.Glyph, out var level))
        {
            level = stroke.Ramp.NearestLevel(cell.Glyph, _densities());
            stroke.Levels.Add(cell.Glyph, level);
        }

        return level;
    }

    // A cell a stroke has reached: as it was before the stroke, and the greatest weight that has reached it.
    private readonly record struct ReachedCell(Cell Cell, double Weight);

    // A stroke's settings, taken when it starts, so it paints one ramp with one tip and one strength throughout.
    private sealed class Stroke(Ramp ramp, double intensity, BrushFalloff falloff, CellColor foreground,
        CellColor background)
    {
        public Ramp Ramp { get; } = ramp;

        public double Intensity { get; } = intensity;

        public BrushFalloff Falloff { get; } = falloff;

        public CellColor Foreground { get; } = foreground;

        public CellColor Background { get; } = background;

        public CoalescingGroup Undo { get; } = new();

        public Dictionary<CellPoint, ReachedCell> Reached { get; } = [];

        // Each glyph's level, measured once per stroke.
        public Dictionary<Rune, int> Levels { get; } = [];

        public CellPoint LastTopLeft { get; set; }

        // Where the history stood after this stroke's last edit, or null before its first. Esc only takes back the
        // newest step if it's still this stroke's.
        public int? UndoCountAfterLastEdit { get; set; }
    }
}
