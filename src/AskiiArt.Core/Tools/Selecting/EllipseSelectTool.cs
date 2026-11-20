// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Selection;

namespace AskiiArt.Core.Tools.Selecting;

/// <summary>
/// Ellipse Select (ARCHITECTURE §10, ADR 0060): dragging from one cell to another selects the ellipse that fits the
/// box between them, both included, combined with the selection by the mode. A cell is in the ellipse when its centre
/// is. It works like Rectangle Select otherwise: Shift during the drag makes a circle, Alt draws from the middle, a
/// click deselects when replacing, and Esc cancels.
/// </summary>
public sealed class EllipseSelectTool : ITool
{
    /// <summary>The tool's id.</summary>
    public const string Id = "askiiart.select.ellipse";

    private readonly BoxSelectGesture _gesture;

    /// <summary>Creates the tool for <paramref name="context"/>'s session.</summary>
    public EllipseSelectTool(ToolContext context)
    {
        _gesture = new BoxSelectGesture(context, SelectionShapes.Ellipse, UndoStepNames.EllipseSelect);
        _gesture.OverlayChanged += (_, _) => OverlayChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Raised when a drag starts showing an ellipse, the ellipse changes, or the drag ends.</summary>
    public event EventHandler? OverlayChanged;

    /// <summary>The tool's identity. The name key is the App's resource key.</summary>
    public static ToolDescriptor ToolDescriptor { get; } =
        new(Id, "ToolEllipseSelect", "tool.select.ellipse", "select");

    /// <summary>Registers Ellipse Select, as DI does.</summary>
    public static ToolDefinition Definition { get; } = new(ToolDescriptor, context => new EllipseSelectTool(context))
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

    /// <summary>The ellipse being dragged, as the selection it would make.</summary>
    public ToolOverlay Overlay => _gesture.Overlay;

    /// <summary>Starts a drag. The selection stays as it is until the pointer reaches another cell.</summary>
    public void PointerPressed(ToolPointerInput input) => _gesture.Press(input);

    /// <summary>Stretches the ellipse's box to the cell under the pointer, which may be off the canvas.</summary>
    public void PointerMoved(ToolPointerInput input) => _gesture.Move(input);

    /// <summary>Selects the ellipse, or deselects after a click, as one undo step.</summary>
    public void PointerReleased(ToolPointerInput input) => _gesture.Release(input);

    /// <summary>Reshapes the ellipse as Shift or Alt go down or up during a drag.</summary>
    public void ModifiersChanged(ToolModifiers modifiers) => _gesture.ModifiersChanged(modifiers);

    /// <summary>Esc during a drag cancels it; otherwise Esc isn't the tool's (ADR 0041).</summary>
    public bool KeyPressed(ToolKey key, ToolModifiers modifiers) => _gesture.KeyPressed(key);

    /// <summary>Takes no text.</summary>
    public bool TextEntered(string text) => false;

    /// <summary>Cancels a drag in progress, leaving the selection as it was.</summary>
    public void Deactivate() => _gesture.Deactivate();
}
