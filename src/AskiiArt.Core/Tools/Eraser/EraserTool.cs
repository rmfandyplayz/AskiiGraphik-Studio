// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Tools.Footprints;
using AskiiArt.Core.Tools.Options;

namespace AskiiArt.Core.Tools.Eraser;

/// <summary>
/// The hard erasers (ARCHITECTURE §8.3, ADR 0050): the Eraser, which empties the cells under its tip on the active
/// layer, and the BG Eraser, which takes their backgrounds away and leaves their glyphs. The tip is the Eraser group's
/// (ADR 0047), and [ and ] change its size. The erasing shows as it happens, so each move is its own command, and the
/// moves of one stroke coalesce into one undo step, as a burst of typing does (ADR 0033).
/// </summary>
public sealed class EraserTool : ITool
{
    /// <summary>The Eraser's id.</summary>
    public const string Id = "askiiart.eraser";

    /// <summary>The BG Eraser's id.</summary>
    public const string BackgroundId = "askiiart.eraser.background";

    /// <summary>The App's resource key for the warning the BG Eraser shows in a document without colours.</summary>
    public const string NoBackgroundsWarningKey = "ToolWarningNoBackgrounds";

    /// <summary>The Eraser's identity. The name key is the App's resource key.</summary>
    public static ToolDescriptor ToolDescriptor { get; } = new(Id, "ToolEraser", "tool.eraser", "paint");

    /// <summary>The BG Eraser's identity. The name key is the App's resource key.</summary>
    public static ToolDescriptor BackgroundDescriptor { get; } =
        new(BackgroundId, "ToolBackgroundEraser", "tool.eraser.background", "paint");

    /// <summary>The Eraser group's tip: its size, shape and proportions, which the Soft Eraser shares.</summary>
    public static ToolOptionGroup OptionGroup { get; } = BrushTipOptions.CreateGroup(Id);

    /// <summary>Registers the Eraser, as DI does.</summary>
    public static ToolDefinition Definition { get; } = CreateDefinition(EraserTarget.Everything);

    /// <summary>Registers the BG Eraser, as DI does.</summary>
    public static ToolDefinition BackgroundDefinition { get; } = CreateDefinition(EraserTarget.Background) with
    {
        WarningFor = BackgroundWarningFor
    };

    private readonly EraserTarget _target;
    private readonly EditingSession _session;
    private readonly BrushTipSource _tip;
    private ToolOverlay _overlay = ToolOverlay.None;
    private CoalescingGroup? _stroke;

    // The footprint a stroke started with, which it keeps throughout, and where its top-left last was.
    private BrushFootprint? _strokeFootprint;
    private CellPoint _lastTopLeft;

    // Where the history stood after this stroke's last edit, or null before its first. Esc only takes
    // back the newest step if it's still this stroke's.
    private int? _undoCountAfterLastEdit;

    /// <summary>
    /// Creates an eraser for <paramref name="session"/> that erases the <paramref name="target"/> part of cells with
    /// <paramref name="tip"/>'s footprint. Call <see cref="Deactivate"/> when done with it, so it stops following the
    /// tip's settings.
    /// </summary>
    public EraserTool(EraserTarget target, EditingSession session, BrushTipSource tip)
    {
        if (!Enum.IsDefined(target))
            throw new ArgumentOutOfRangeException(nameof(target), target, null);

        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(tip);

        _target = target;
        _session = session;
        _tip = tip;
        _tip.Values.Changed += OnTipChanged;
    }

    /// <summary>Raised when the tip's size or shape changes, so the canvas outlines the new one.</summary>
    public event EventHandler? OverlayChanged;

    /// <inheritdoc/>
    public ToolDescriptor Descriptor => _target == EraserTarget.Everything ? ToolDescriptor : BackgroundDescriptor;

    /// <summary>True: Space + drag pans.</summary>
    public bool AllowsSpacePanning => true;

    /// <summary>False: dragging erases.</summary>
    public bool PansWithPrimaryButton => false;

    /// <summary>False: the eraser takes no text.</summary>
    public bool IsTakingText => false;

    /// <summary>The tip's footprint, for the canvas to outline under the pointer.</summary>
    public ToolOverlay Overlay
    {
        get
        {
            var footprint = _strokeFootprint ?? _tip.Footprint;
            if (_overlay.Footprint != footprint)
                _overlay = new ToolOverlay(Caret: null, footprint);

            return _overlay;
        }
    }

    /// <summary>Starts a stroke, unless the active layer is locked or hidden. It may start outside the canvas.</summary>
    public void PointerPressed(ToolPointerInput input)
    {
        if (!_session.CanEditActiveLayer)
            return;

        _stroke = new CoalescingGroup();
        _strokeFootprint = _tip.Footprint;
        _undoCountAfterLastEdit = null;
        _lastTopLeft = _strokeFootprint.TopLeftAt(input);
        Erase(_lastTopLeft, _lastTopLeft);
    }

    /// <summary>Continues the stroke in a straight line, because a fast drag reports cells far apart.</summary>
    public void PointerMoved(ToolPointerInput input)
    {
        if (_stroke is null || _strokeFootprint is null)
            return;

        var topLeft = _strokeFootprint.TopLeftAt(input);
        if (topLeft == _lastTopLeft)
            return;

        Erase(_lastTopLeft, topLeft);
        _lastTopLeft = topLeft;
    }

    /// <inheritdoc/>
    public void PointerReleased(ToolPointerInput input) => EndStroke();

    /// <summary>
    /// Esc during a stroke takes it back as if it never happened. Otherwise Esc isn't the eraser's, so the
    /// App switches to the Hand tool (ADR 0041).
    /// </summary>
    public bool KeyPressed(ToolKey key, ToolModifiers modifiers)
    {
        if (key != ToolKey.Escape || _stroke is null)
            return false;

        if (_undoCountAfterLastEdit == _session.History.UndoCount)
            _session.History.CancelNewestStep();

        EndStroke();
        return true;
    }

    /// <summary>[ makes the tip a cell smaller, and ] a cell bigger.</summary>
    public bool TextEntered(string text) => BrushTipOptions.TryUseSizeKey(_tip.Values, text);

    /// <summary>Keeps what the stroke has erased so far, and stops following the tip's settings.</summary>
    public void Deactivate()
    {
        EndStroke();
        _tip.Values.Changed -= OnTipChanged;
    }

    // What erasing leaves of a cell.
    private Cell Erased(Cell cell) => _target switch
    {
        EraserTarget.Everything => Cell.Empty,
        _ => cell.WithBackground(CellColor.Unset)
    };

    private static ToolDefinition CreateDefinition(EraserTarget target) => new(
        target == EraserTarget.Everything ? ToolDescriptor : BackgroundDescriptor,
        context => new EraserTool(target, context.Session,
            new BrushTipSource(context.Settings.For(OptionGroup), context.CellWidthToHeight)))
    {
        OptionGroups = [OptionGroup]
    };

    // A monochrome document's cells have no backgrounds, so the BG Eraser has nothing to erase there.
    private static string? BackgroundWarningFor(ToolSettings settings, Document document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return document.ColorMode == ColorMode.Monochrome ? NoBackgroundsWarningKey : null;
    }

    private void OnTipChanged(object? sender, EventArgs e) => OverlayChanged?.Invoke(this, EventArgs.Empty);

    private void EndStroke()
    {
        var hadStroke = _strokeFootprint is not null;
        _stroke = null;
        _strokeFootprint = null;

        // A size changed during the stroke shows only now.
        if (hadStroke && _overlay.Footprint != _tip.Footprint)
            OverlayChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Erase(CellPoint fromTopLeft, CellPoint toTopLeft)
    {
        // Undoing during a stroke can take away the layer it started on.
        if (!_session.CanEditActiveLayer || _strokeFootprint is null)
            return;

        var document = _session.Document;
        var layer = _session.ActiveLayer;
        var cells = _session.EditCellsInSelection(layer.Id);
        foreach (var point in FootprintSweep.Cover(_strokeFootprint, fromTopLeft, toTopLeft, document.CanvasSize))
            cells.Set(point, Erased(layer.GetCell(point)));

        if (cells.Build(UndoStepNames.Erase) is not { } command)
            return;

        _session.History.Execute(command, _stroke);
        _undoCountAfterLastEdit = _session.History.UndoCount;
    }
}
