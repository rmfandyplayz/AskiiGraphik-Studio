// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Selection;

namespace AskiiArt.Core.Tools.Selecting;

/// <summary>
/// The Lasso (ARCHITECTURE §10, ADR 0060): dragging draws a freehand path, and releasing selects every cell whose
/// centre it goes round, the end joined back to the start, combined with the selection by the mode. While it's drawn,
/// the canvas shows the path, faintly how it would close, and what letting go now would select (ADR 0061). A path of
/// fewer than three points goes round nothing and counts as a click, which deselects when replacing. Esc during the
/// drag cancels it.
/// </summary>
public sealed class LassoTool : ITool
{
    /// <summary>The tool's id.</summary>
    public const string Id = "askiiart.select.lasso";

    // How far, in cells, the pointer moves before the path takes another point, which keeps a long path small.
    private const double PointSpacing = 0.2;

    // A path needs this many points to go round anything; one with fewer is a click.
    private const int FewestPointsOfAShape = 3;

    private readonly EditingSession _session;
    private readonly SelectionCombiner _combiner;
    private readonly List<CanvasPoint> _path = [];
    private ToolOverlay _overlay = ToolOverlay.None;
    private bool _isDrawing;

    /// <summary>Creates the tool for <paramref name="context"/>'s session.</summary>
    public LassoTool(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _session = context.Session;
        _combiner = new SelectionCombiner(context.Session, context.Settings.For(SelectionToolOptions.Group),
            ShowPreview);
    }

    /// <summary>
    /// Raised when the path grows, an undo or redo changes what letting go would select, or the drag ends.
    /// </summary>
    public event EventHandler? OverlayChanged;

    /// <summary>The tool's identity. The name key is the App's resource key.</summary>
    public static ToolDescriptor ToolDescriptor { get; } = new(Id, "ToolLasso", "tool.select.lasso", "select");

    /// <summary>Registers the Lasso, as DI does.</summary>
    public static ToolDefinition Definition { get; } = new(ToolDescriptor, context => new LassoTool(context))
    {
        OptionGroups = [SelectionToolOptions.Group]
    };

    /// <inheritdoc/>
    public ToolDescriptor Descriptor => ToolDescriptor;

    /// <summary>True: Space + drag pans.</summary>
    public bool AllowsSpacePanning => true;

    /// <summary>False: dragging draws the path.</summary>
    public bool PansWithPrimaryButton => false;

    /// <summary>False: the tool takes no text.</summary>
    public bool IsTakingText => false;

    /// <summary>The path drawn so far, and what letting go now would select.</summary>
    public ToolOverlay Overlay => _overlay;

    /// <summary>Starts a path where the pointer is.</summary>
    public void PointerPressed(ToolPointerInput input)
    {
        _path.Clear();
        _path.Add(input.Position);
        _isDrawing = true;
        _combiner.Start(input.Modifiers);
    }

    /// <summary>Takes the path on to where the pointer is, during a drag.</summary>
    public void PointerMoved(ToolPointerInput input)
    {
        if (!_isDrawing)
            return;

        var last = _path[^1];
        var position = input.Position;
        if (Math.Abs(position.X - last.X) < PointSpacing && Math.Abs(position.Y - last.Y) < PointSpacing)
            return;

        _path.Add(position);
        ShowPath();
    }

    /// <summary>Closes the path and selects what it goes round, as one undo step.</summary>
    public void PointerReleased(ToolPointerInput input)
    {
        if (!_isDrawing)
            return;

        PointerMoved(input);
        var shape = ShapeOf(_session.Document.CanvasSize, _path);
        End();
        _combiner.Finish(shape, UndoStepNames.Lasso);
    }

    /// <summary>
    /// Esc during a drag cancels it, leaving the selection as it was. Otherwise Esc isn't the tool's (ADR 0041).
    /// </summary>
    public bool KeyPressed(ToolKey key, ToolModifiers modifiers)
    {
        if (key != ToolKey.Escape || !_isDrawing)
            return false;

        End();
        return true;
    }

    /// <summary>Takes no text.</summary>
    public bool TextEntered(string text) => false;

    /// <summary>Cancels a path in progress, leaving the selection as it was.</summary>
    public void Deactivate() => End();

    // The canvas works the shape out from a copy of the path, and only if it draws this overlay, at most once a frame.
    private void ShowPath()
    {
        IReadOnlyList<CanvasPoint> path = [.. _path];
        SetOverlay(new ToolOverlay(Caret: null,
            SelectionPreview: _combiner.Preview(() => ShapeOf(_session.Document.CanvasSize, path)),
            Path: path,
            ShowsPathClosing: path.Count > 2));
    }

    private static SelectionMask? ShapeOf(CanvasSize canvasSize, IReadOnlyList<CanvasPoint> path)
    {
        if (path.Count < FewestPointsOfAShape)
            return null;

        return SelectionShapes.Polygon(canvasSize, path);
    }

    private void End()
    {
        _isDrawing = false;
        _path.Clear();
        _combiner.Stop();
        SetOverlay(ToolOverlay.None);
    }

    // An undo or redo during the drag changed the selection the path combines with.
    private void ShowPreview(SelectionPreview preview) => SetOverlay(_overlay with { SelectionPreview = preview });

    private void SetOverlay(ToolOverlay overlay)
    {
        if (overlay == _overlay)
            return;

        _overlay = overlay;
        OverlayChanged?.Invoke(this, EventArgs.Empty);
    }
}
