// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Tools;

/// <summary>
/// A tool's identity, for menus, toolbars and settings (ARCHITECTURE §8.1).
/// </summary>
/// <param name="Id">A stable id, for example "askiiart.text". Never shown to the user.</param>
/// <param name="NameKey">The App's resource key for the tool's localized name.</param>
/// <param name="IconKey">The tool's icon key in the icon lookup (§14.5).</param>
/// <param name="Category">Groups tools in the tool palette, for example "text" or "paint".</param>
public sealed record ToolDescriptor(string Id, string NameKey, string IconKey, string Category);

/// <summary>
/// A canvas tool (ARCHITECTURE §8.1). One instance edits one <see cref="Editing.EditingSession"/>,
/// receiving input in cell coordinates and changing the document only through commands.
/// </summary>
public interface ITool
{
    /// <summary>The tool's identity.</summary>
    ToolDescriptor Descriptor { get; }

    /// <summary>
    /// Whether Space + drag pans the canvas while this tool is active. A tool that types text
    /// says no, so Space types a space instead (ADR 0017).
    /// </summary>
    bool AllowsSpacePanning { get; }

    /// <summary>What the canvas draws on top of the document for this tool.</summary>
    ToolOverlay Overlay { get; }

    /// <summary>Raised when <see cref="Overlay"/> changes.</summary>
    event EventHandler? OverlayChanged;

    /// <summary>The primary pointer button went down.</summary>
    void PointerPressed(ToolPointerInput input);

    /// <summary>The pointer moved, with or without the button held.</summary>
    void PointerMoved(ToolPointerInput input);

    /// <summary>The primary pointer button came up.</summary>
    void PointerReleased(ToolPointerInput input);

    /// <summary>A key went down. Returns true when the tool used it.</summary>
    bool KeyPressed(ToolKey key, ToolModifiers modifiers);

    /// <summary>The user typed <paramref name="text"/>. Returns true when the tool used it.</summary>
    bool TextEntered(string text);

    /// <summary>Another tool is taking over. Ends any gesture in progress.</summary>
    void Deactivate();
}
