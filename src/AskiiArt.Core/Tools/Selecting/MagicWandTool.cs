// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Ramps;
using AskiiArt.Core.Selection;
using AskiiArt.Core.Tools.Options;

namespace AskiiArt.Core.Tools.Selecting;

/// <summary>
/// The Magic Wand (ARCHITECTURE §10, ADR 0063): clicking a cell selects the cells that match it, combined with the
/// selection by the mode. Its options choose what cells are compared by, how far they may differ, whether only the
/// matching cells joined to the clicked one along their sides count or every one, and whether it looks at the active
/// layer or at every visible layer together. The canvas previews the result as soon as the button goes down; dragging
/// left or right before letting go lowers or raises the tolerance, and the preview follows. Letting go makes it one
/// undo step, and Esc before then cancels it and puts the tolerance back. It only selects, so it works on a locked or
/// hidden layer too.
/// </summary>
public sealed class MagicWandTool : ITool
{
    /// <summary>The tool's id.</summary>
    public const string Id = "askiiart.select.wand";

    /// <summary>How far the pointer moves sideways, in logical pixels, to change the tolerance by 1 percent.</summary>
    public const double PixelsPerToleranceStep = 2;

    private readonly EditingSession _session;
    private readonly ToolOptionValues _options;
    private readonly Func<IGlyphDensityProvider> _densities;
    private readonly SelectionCombiner _combiner;
    private readonly SelectionArrows _arrows;

    // The click in progress, or null when there's none.
    private Click? _click;

    // What the click compares, taken when it's first needed and again after the document changes.
    private MagicWandSample? _sample;

    // The shape last worked out, and the tolerance it was worked out for.
    private SelectionMask? _shape;
    private int _shapeTolerance;

    /// <summary>Creates the tool for <paramref name="context"/>'s session.</summary>
    public MagicWandTool(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _session = context.Session;
        _options = context.Settings.For(MagicWandOptions.Group);
        _densities = context.Densities;
        _combiner = new SelectionCombiner(context.Session, context.Settings.For(SelectionToolOptions.Group),
            ShowPreview);
        _arrows = new SelectionArrows(context);
    }

    /// <summary>
    /// Raised when a click starts showing what it would select, a drag changes the tolerance, an undo or redo changes
    /// what letting go would select, or the click ends.
    /// </summary>
    public event EventHandler? OverlayChanged;

    /// <summary>The tool's identity. The name key is the App's resource key.</summary>
    public static ToolDescriptor ToolDescriptor { get; } = new(Id, "ToolMagicWand", "tool.select.wand", "select");

    /// <summary>Registers the Magic Wand, as DI does.</summary>
    public static ToolDefinition Definition { get; } = new(ToolDescriptor, context => new MagicWandTool(context))
    {
        OptionGroups = [SelectionToolOptions.Group, MagicWandOptions.Group]
    };

    /// <inheritdoc/>
    public ToolDescriptor Descriptor => ToolDescriptor;

    /// <summary>True: Space + drag pans.</summary>
    public bool AllowsSpacePanning => true;

    /// <summary>False: clicking selects.</summary>
    public bool PansWithPrimaryButton => false;

    /// <summary>False: the tool takes no text.</summary>
    public bool IsTakingText => false;

    /// <summary>What the click would select, while the button is down.</summary>
    public ToolOverlay Overlay { get; private set; } = ToolOverlay.None;

    /// <summary>
    /// Starts a click on the cell under the pointer and previews what it would select. A cell off the canvas selects
    /// nothing, so letting go there acts as a click on nothing does: it deselects when replacing.
    /// </summary>
    public void PointerPressed(ToolPointerInput input)
    {
        End();
        _arrows.EndMove();
        var settings = MagicWandOptions.Read(_options);
        _click = new Click(input.Cell, input.Position.X, settings);
        _session.Document.Changed += OnDocumentChanged;
        _session.ActiveLayerChanged += OnActiveLayerChanged;
        _combiner.Start(input.Modifiers);
        UpdatePreview();
    }

    /// <summary>
    /// Dragging right raises the tolerance and dragging left lowers it, one percent every
    /// <see cref="PixelsPerToleranceStep"/> logical pixels from where the button went down.
    /// </summary>
    public void PointerMoved(ToolPointerInput input)
    {
        if (_click is not { } click)
            return;

        var pixels = (input.Position.X - click.StartX) * input.CellWidthOnScreen;
        var steps = (int)(pixels / PixelsPerToleranceStep);
        _options.Set(MagicWandOptions.Tolerance, click.Settings.Tolerance + steps);
        UpdatePreview();
    }

    /// <summary>Selects what the click matches, combined by the mode, as one undo step.</summary>
    public void PointerReleased(ToolPointerInput input)
    {
        if (_click is null)
            return;

        var shape = Shape(Tolerance);
        End();
        _combiner.Finish(shape, UndoStepNames.MagicWand);
    }

    /// <summary>
    /// Esc during a click cancels it, leaving the selection as it was and the tolerance as it was when the button went
    /// down. Otherwise Esc isn't the tool's, so the App switches to the Hand tool (ADR 0041).
    /// With something selected and the button up, the arrows move the selected cells as the Move tool's do, and Enter
    /// ends that move and deselects (ADR 0073).
    /// </summary>
    public bool KeyPressed(ToolKey key, ToolModifiers modifiers)
    {
        if (key != ToolKey.Escape || _click is null)
            return _arrows.KeyPressed(key, isGestureUnderWay: _click is not null);

        Cancel();
        return true;
    }

    /// <summary>Notes when an arrow is let go, which decides whether the next press starts a new step.</summary>
    public void ArrowReleased(ToolKey arrow) => _arrows.ArrowReleased(arrow);

    /// <summary>Takes no text.</summary>
    public bool TextEntered(string text) => false;

    /// <summary>
    /// Cancels a click in progress, leaving the selection and the tolerance as they were, and ends a move the arrows
    /// made.
    /// </summary>
    public void Deactivate()
    {
        Cancel();
        _arrows.Detach();
    }

    private int Tolerance => _options.Get(MagicWandOptions.Tolerance);

    private void UpdatePreview()
    {
        // The canvas works the shape out only if it draws this overlay.
        var tolerance = Tolerance;
        SetOverlay(new ToolOverlay(Caret: null, SelectionPreview: _combiner.Preview(() => Shape(tolerance))));
    }

    // The cells the click matches at the tolerance, or null when it's off the canvas, as it may be after an undo of a
    // resize.
    private SelectionMask? Shape(int tolerance)
    {
        if (_click is not { } click)
            return null;

        var canvasSize = _session.Document.CanvasSize;
        if (!canvasSize.Contains(click.Cell))
            return null;

        if (_shape is not null && _shapeTolerance == tolerance && _sample?.CanvasSize == canvasSize)
            return _shape;

        if (_sample is null || _sample.CanvasSize != canvasSize)
        {
            _sample = MagicWandSample.Take(_session, click.Settings.SamplesVisibleLayers, click.Settings.Match,
                _densities());
        }

        _shape = _sample.Select(click.Cell, tolerance, click.Settings.IsContiguous);
        _shapeTolerance = tolerance;
        return _shape;
    }

    private void Cancel()
    {
        if (_click is not { } click)
            return;

        End();
        _options.Set(MagicWandOptions.Tolerance, click.Settings.Tolerance);
    }

    private void End()
    {
        if (_click is not null)
        {
            _session.Document.Changed -= OnDocumentChanged;
            _session.ActiveLayerChanged -= OnActiveLayerChanged;
        }

        _click = null;
        ForgetSample();
        _combiner.Stop();
        SetOverlay(ToolOverlay.None);
    }

    // An undo or redo during the click changed the cells it compares, or which layer it compares on.
    private void OnDocumentChanged(object? sender, DocumentChangedEventArgs e) => Resample();

    private void OnActiveLayerChanged(object? sender, EventArgs e) => Resample();

    private void Resample()
    {
        ForgetSample();
        UpdatePreview();
    }

    private void ForgetSample()
    {
        _sample = null;
        _shape = null;
    }

    // An undo or redo during the click changed the selection the shape combines with.
    private void ShowPreview(SelectionPreview preview) => SetOverlay(Overlay with { SelectionPreview = preview });

    private void SetOverlay(ToolOverlay overlay)
    {
        if (overlay == Overlay)
            return;

        Overlay = overlay;
        OverlayChanged?.Invoke(this, EventArgs.Empty);
    }

    // A click: the cell it started on, where across the canvas the pointer was, in cells, and the options then.
    private readonly record struct Click(CellPoint Cell, double StartX, MagicWandSettings Settings);
}
