// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Selection;

namespace AskiiArt.Core.Tools.Selecting;

/// <summary>
/// Rectangle Select (ARCHITECTURE §10, ADR 0055, ADR 0060): dragging from one cell to another selects the rectangle
/// between them, both included, combined with the selection by the mode; a click that doesn't move to another cell
/// deselects when replacing, and otherwise does nothing. The canvas outlines the result while it's dragged, and
/// releasing makes it one undo step; Esc during the drag cancels it. Shift during the drag makes a square, and Alt
/// draws from the middle. It only selects, so it works on a locked or hidden layer too.
/// </summary>
public sealed class RectangleSelectTool : ITool
{
    /// <summary>The tool's id.</summary>
    public const string Id = "askiiart.select.rectangle";

    private readonly BoxSelectGesture _gesture;

    /// <summary>Creates the tool for <paramref name="context"/>'s session.</summary>
    public RectangleSelectTool(ToolContext context)
    {
        _gesture = new BoxSelectGesture(context, SelectionMask.Rectangle, UndoStepNames.RectangleSelect);
        _gesture.OverlayChanged += (_, _) => OverlayChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Raised when a drag starts showing a rectangle, the rectangle changes, an undo or redo changes what letting go
    /// would select, or the drag ends.
    /// </summary>
    public event EventHandler? OverlayChanged;

    /// <summary>The tool's identity. The name key is the App's resource key.</summary>
    public static ToolDescriptor ToolDescriptor { get; } =
        new(Id, "ToolRectangleSelect", "tool.select.rectangle", "select");

    /// <summary>Registers Rectangle Select, as DI does.</summary>
    public static ToolDefinition Definition { get; } = new(ToolDescriptor, context => new RectangleSelectTool(context))
    {
        OptionGroups = [SelectionToolOptions.Group]
    };

    /// <inheritdoc/>
    public ToolDescriptor Descriptor => ToolDescriptor;

    /// <summary>True: Space + drag pans.</summary>
    public bool AllowsSpacePanning => true;

    /// <summary>False: dragging selects.</summary>
    public bool PansWithPrimaryButton => false;

    /// <summary>False: the tool takes no text.</summary>
    public bool IsTakingText => false;

    /// <summary>The rectangle being dragged, as the selection it would make.</summary>
    public ToolOverlay Overlay => _gesture.Overlay;

    /// <summary>Starts a drag. The selection stays as it is until the pointer reaches another cell.</summary>
    public void PointerPressed(ToolPointerInput input) => _gesture.Press(input);

    /// <summary>Stretches the rectangle to the cell under the pointer, which may be off the canvas.</summary>
    public void PointerMoved(ToolPointerInput input) => _gesture.Move(input);

    /// <summary>Selects the rectangle, or deselects after a click, as one undo step.</summary>
    public void PointerReleased(ToolPointerInput input) => _gesture.Release(input);

    /// <summary>Reshapes the rectangle as Shift or Alt go down or up during a drag.</summary>
    public void ModifiersChanged(ToolModifiers modifiers) => _gesture.ModifiersChanged(modifiers);

    /// <summary>
    /// Esc during a drag cancels it, leaving the selection as it was. Otherwise Esc isn't the tool's, so the App
    /// switches to the Hand tool (ADR 0041).
    /// With something selected and no drag under way, the arrows move the selected cells as the Move tool's do, and
    /// Enter ends that move and deselects (ADR 0073).
    /// </summary>
    public bool KeyPressed(ToolKey key, ToolModifiers modifiers) => _gesture.KeyPressed(key);

    /// <summary>Notes when an arrow is let go, which decides whether the next press starts a new step.</summary>
    public void ArrowReleased(ToolKey arrow) => _gesture.ArrowReleased(arrow);

    /// <summary>Takes no text.</summary>
    public bool TextEntered(string text) => false;

    /// <summary>Cancels a drag in progress, leaving the selection as it was, and ends a move the arrows made.</summary>
    public void Deactivate() => _gesture.Deactivate();
}
