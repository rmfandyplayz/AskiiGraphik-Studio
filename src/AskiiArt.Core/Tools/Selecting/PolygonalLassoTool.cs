// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Selection;

namespace AskiiArt.Core.Tools.Selecting;

/// <summary>
/// The Polygonal Lasso (ARCHITECTURE §10, ADR 0060): each click adds a corner, and a line follows the pointer from the
/// last one. Clicking the first corner's cell again, double-clicking or pressing Enter selects every cell whose centre
/// the shape goes round, combined with the selection by the mode, as one undo step. Backspace takes the last corner
/// away, and Esc cancels.
/// </summary>
public sealed class PolygonalLassoTool : ITool
{
    /// <summary>The tool's id.</summary>
    public const string Id = "askiiart.select.polygon";

    private readonly EditingSession _session;
    private readonly SelectionCombiner _combiner;
    private readonly List<CanvasPoint> _corners = [];
    private CanvasPoint _pointer;
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

    /// <summary>The corners so far, and the line from the last one to the pointer.</summary>
    public ToolOverlay Overlay => _overlay;

    /// <summary>
    /// Adds a corner where the pointer is, starting a shape if there's none. The second click of a double-click, or a
    /// click in the first corner's cell, finishes the shape instead.
    /// </summary>
    public void PointerPressed(ToolPointerInput input)
    {
        _pointer = input.Position;
        if (!IsDrawing)
        {
            _combiner.Start(input.Modifiers);
            _corners.Add(_pointer);
            ShowPath();
            return;
        }

        if (input.ClickCount >= 2 || (_corners.Count > 2 && input.Cell == _corners[0].Cell))
        {
            Finish();
            return;
        }

        _corners.Add(_pointer);
        ShowPath();
    }

    /// <summary>Moves the line from the last corner to the pointer.</summary>
    public void PointerMoved(ToolPointerInput input)
    {
        _pointer = input.Position;
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
        var shape = SelectionShapes.Polygon(_session.Document.CanvasSize, _corners);
        Cancel();
        _combiner.Finish(shape, UndoStepNames.PolygonalLasso);
    }

    private void Cancel()
    {
        _corners.Clear();
        ShowPath();
    }

    private void ShowPath() =>
        SetOverlay(IsDrawing ? new ToolOverlay(Caret: null, Path: [.. _corners, _pointer]) : ToolOverlay.None);

    private void SetOverlay(ToolOverlay overlay)
    {
        if (overlay == _overlay)
            return;

        _overlay = overlay;
        OverlayChanged?.Invoke(this, EventArgs.Empty);
    }
}
