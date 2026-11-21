// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Selection;

namespace AskiiArt.Core.Tools.Selecting;

/// <summary>
/// The Polygonal Lasso (ARCHITECTURE §10, ADR 0060): each click adds a corner, and a line follows the pointer from the
/// last one. Clicking the first corner's marker, double-clicking or pressing Enter selects every cell whose centre the
/// shape goes round, combined with the selection by the mode, as one undo step. Backspace takes the last corner away,
/// and Esc cancels. While it's drawn, the canvas shows a marker on each corner, faintly how the shape would close, and
/// what a double-click where the pointer is would select (ADR 0061). A shape of fewer than three corners goes round
/// nothing and counts as a click, which deselects when replacing.
/// </summary>
public sealed class PolygonalLassoTool : ITool
{
    /// <summary>The tool's id.</summary>
    public const string Id = "askiiart.select.polygon";

    // A shape needs this many corners to go round anything; one with fewer is a click.
    private const int FewestCornersOfAShape = 3;

    private readonly EditingSession _session;
    private readonly SelectionCombiner _combiner;
    private readonly List<CanvasPoint> _corners = [];
    private CanvasPoint _pointer;
    private ToolMarkerReach _reach;
    private ToolOverlay _overlay = ToolOverlay.None;

    /// <summary>Creates the tool for <paramref name="context"/>'s session.</summary>
    public PolygonalLassoTool(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _session = context.Session;
        _combiner = new SelectionCombiner(context.Session, context.Settings.For(SelectionToolOptions.Group));
    }

    /// <summary>
    /// Raised when a corner is added or taken away, the pointer moves while there are corners, or the shape ends.
    /// </summary>
    public event EventHandler? OverlayChanged;

    /// <summary>The tool's identity. The name key is the App's resource key.</summary>
    public static ToolDescriptor ToolDescriptor { get; } =
        new(Id, "ToolPolygonalLasso", "tool.select.polygon", "select");

    /// <summary>Registers the Polygonal Lasso, as DI does.</summary>
    public static ToolDefinition Definition { get; } = new(ToolDescriptor, context => new PolygonalLassoTool(context))
    {
        OptionGroups = [SelectionToolOptions.Group]
    };

    /// <inheritdoc/>
    public ToolDescriptor Descriptor => ToolDescriptor;

    /// <summary>True: Space + drag pans, between clicks too.</summary>
    public bool AllowsSpacePanning => true;

    /// <summary>False: clicking adds corners.</summary>
    public bool PansWithPrimaryButton => false;

    /// <summary>False: the tool takes no text.</summary>
    public bool IsTakingText => false;

    /// <summary>True while a shape is being drawn, so Enter, Backspace and Esc are the tool's.</summary>
    public bool IsDrawing => _corners.Count > 0;

    /// <summary>The corners, the line from the last one to the pointer, and what the shape would select.</summary>
    public ToolOverlay Overlay => _overlay;

    // True when a click now would finish the shape on its first corner: there are enough corners to make one, and the
    // pointer is on the first corner's marker.
    private bool IsPointerOnFinish => _corners.Count >= FewestCornersOfAShape && _reach.Covers(_corners[0], _pointer);

    /// <summary>
    /// Adds a corner where the pointer is, starting a shape if there's none. The second click of a double-click, or a
    /// click on the first corner's marker, finishes the shape instead.
    /// </summary>
    public void PointerPressed(ToolPointerInput input)
    {
        _pointer = input.Position;
        _reach = input.MarkerReach;
        if (!IsDrawing)
        {
            _combiner.Start(input.Modifiers);
            _corners.Add(_pointer);
            ShowPath();
            return;
        }

        if (input.ClickCount >= 2 || IsPointerOnFinish)
        {
            Finish();
            return;
        }

        _corners.Add(_pointer);
        ShowPath();
    }

    /// <summary>
    /// Moves the line from the last corner to the pointer. On the first corner's marker, the line joins it instead.
    /// </summary>
    public void PointerMoved(ToolPointerInput input)
    {
        _pointer = input.Position;
        _reach = input.MarkerReach;
        if (IsDrawing)
            ShowPath();
    }

    /// <summary>Does nothing: corners are added when the button goes down.</summary>
    public void PointerReleased(ToolPointerInput input)
    {
    }

    /// <summary>
    /// While drawing, Enter finishes the shape, Backspace takes the last corner away (the first, too, which cancels)
    /// and Esc cancels. Otherwise none of them is the tool's, so Esc switches to the Hand tool (ADR 0041).
    /// </summary>
    public bool KeyPressed(ToolKey key, ToolModifiers modifiers)
    {
        if (!IsDrawing)
            return false;

        switch (key)
        {
            case ToolKey.Enter:
                Finish();
                return true;
            case ToolKey.Backspace:
                _corners.RemoveAt(_corners.Count - 1);
                ShowPath();
                return true;
            case ToolKey.Escape:
                Cancel();
                return true;
            default:
                return false;
        }
    }

    /// <summary>Takes no text.</summary>
    public bool TextEntered(string text) => false;

    /// <summary>Cancels a shape in progress, leaving the selection as it was.</summary>
    public void Deactivate() => Cancel();

    private void Finish()
    {
        var shape = ShapeOf(_session.Document.CanvasSize, _corners);
        Cancel();
        _combiner.Finish(shape, UndoStepNames.PolygonalLasso);
    }

    private void Cancel()
    {
        _corners.Clear();
        ShowPath();
    }

    // The shape is the corners and the pointer, as a double-click there would make it, or the corners alone when a
    // click would finish it on the first corner. The canvas works it out only if it draws this overlay.
    private void ShowPath()
    {
        if (!IsDrawing)
        {
            SetOverlay(ToolOverlay.None);
            return;
        }

        var isClosing = IsPointerOnFinish;
        IReadOnlyList<CanvasPoint> outline = isClosing ? [.. _corners] : [.. _corners, _pointer];
        var canvasSize = _session.Document.CanvasSize;
        SetOverlay(new ToolOverlay(Caret: null,
            SelectionPreview: _combiner.Preview(() => ShapeOf(canvasSize, outline)),
            Path: isClosing ? [.. _corners, _corners[0]] : outline,
            ShowsPathClosing: !isClosing && outline.Count > 2,
            Markers: Markers(isClosing)));
    }

    private static SelectionMask? ShapeOf(CanvasSize canvasSize, IReadOnlyList<CanvasPoint> corners)
    {
        if (corners.Count < FewestCornersOfAShape)
            return null;

        return SelectionShapes.Polygon(canvasSize, corners);
    }

    private ToolMarker[] Markers(bool isClosing)
    {
        var markers = new ToolMarker[_corners.Count];
        markers[0] = new ToolMarker(_corners[0], isClosing ? ToolMarkerKind.FinishUnderPointer : ToolMarkerKind.Finish);
        for (var index = 1; index < _corners.Count; index++)
            markers[index] = new ToolMarker(_corners[index], ToolMarkerKind.Corner);

        return markers;
    }

    private void SetOverlay(ToolOverlay overlay)
    {
        if (overlay == _overlay)
            return;

        _overlay = overlay;
        OverlayChanged?.Invoke(this, EventArgs.Empty);
    }
}
