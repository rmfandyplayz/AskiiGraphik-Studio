// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Tools.Hand;

/// <summary>
/// The Hand tool (ADR 0041): dragging with the primary button pans the canvas, and it never changes the
/// document. The canvas does the panning, since the view isn't part of Core. Esc switches to it from any
/// other tool, so a tool is always chosen. A Select tool is likely to take its place later.
/// </summary>
public sealed class HandTool : ITool
{
    /// <summary>The tool's id.</summary>
    public const string Id = "askiiart.hand";

    /// <summary>The Hand tool's identity. The name key is the App's resource key.</summary>
    public static ToolDescriptor ToolDescriptor { get; } = new(Id, "ToolHand", "tool.hand", "view");

    /// <summary>Registers the Hand tool, as DI does.</summary>
    public static ToolDefinition Definition { get; } = new(ToolDescriptor, _ => new HandTool());

    /// <summary>The Hand tool never draws anything over the document, so this never happens.</summary>
    public event EventHandler? OverlayChanged
    {
        add { }
        remove { }
    }

    /// <inheritdoc/>
    public ToolDescriptor Descriptor => ToolDescriptor;

    /// <summary>True: Space + drag pans too, as with any tool that takes no text.</summary>
    public bool AllowsSpacePanning => true;

    /// <summary>True: the canvas pans when the primary button drags.</summary>
    public bool PansWithPrimaryButton => true;

    /// <summary>False: the Hand tool takes no text.</summary>
    public bool IsTakingText => false;

    /// <inheritdoc/>
    public ToolOverlay Overlay => ToolOverlay.None;

    /// <summary>Nothing: the canvas pans instead.</summary>
    public void PointerPressed(ToolPointerInput input)
    {
    }

    /// <summary>Nothing: the canvas pans instead.</summary>
    public void PointerMoved(ToolPointerInput input)
    {
    }

    /// <summary>Nothing: the canvas pans instead.</summary>
    public void PointerReleased(ToolPointerInput input)
    {
    }

    /// <summary>Uses no keys.</summary>
    public bool KeyPressed(ToolKey key, ToolModifiers modifiers) => false;

    /// <summary>Takes no text.</summary>
    public bool TextEntered(string text) => false;

    /// <summary>Nothing to end.</summary>
    public void Deactivate()
    {
    }
}
