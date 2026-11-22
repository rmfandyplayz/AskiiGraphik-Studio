// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tools;

/// <summary>
/// A tool's identity, for menus, toolbars and settings (ARCHITECTURE §8.1).
/// </summary>
/// <param name="Id">A stable id, for example "askiiart.text". Never shown to the user.</param>
/// <param name="NameKey">The App's resource key for the tool's localized name.</param>
/// <param name="IconKey">The tool's icon key in the icon lookup (§14.5).</param>
/// <param name="Category">Groups tools in the tool palette, for example "text" or "paint".</param>
/// <remarks>A tool's letter key belongs to its <see cref="ToolGroup"/> (ADR 0049).</remarks>
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

    /// <summary>
    /// Whether dragging with the primary button pans the canvas instead of reaching the tool, as the
    /// Hand tool does (ADR 0041). The tool's pointer methods then aren't called for that button.
    /// </summary>
    bool PansWithPrimaryButton { get; }

    /// <summary>True while typed characters go to this tool, so letter keys can't pick another tool.</summary>
    bool IsTakingText { get; }

    /// <summary>
    /// Whether the pointer shows the move cursor over <paramref name="cell"/>, as it does over the cells a drag of the
    /// Move tool would move (ADR 0067).
    /// </summary>
    bool ShowsMoveCursorAt(CellPoint cell) => false;

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

    /// <summary>
    /// An arrow key went up, reported as <see cref="ToolKey.Left"/>, <see cref="ToolKey.Right"/>,
    /// <see cref="ToolKey.Up"/> or <see cref="ToolKey.Down"/> whatever modifiers are held. A tool that groups presses by
    /// how they're held, as the Move tool's arrow presses are (ADR 0070), uses it.
    /// </summary>
    void ArrowReleased(ToolKey arrow)
    {
    }

    /// <summary>
    /// Shift, Alt or Ctrl (Cmd on macOS) went down or up, so <paramref name="modifiers"/> are held now, though the
    /// pointer hasn't moved. A tool whose gesture follows them, such as a selection drag that Shift makes square or a
    /// move that Shift keeps to an axis, uses it.
    /// </summary>
    void ModifiersChanged(ToolModifiers modifiers)
    {
    }

    /// <summary>The user typed <paramref name="text"/>. Returns true when the tool used it.</summary>
    bool TextEntered(string text);

    /// <summary>Another tool is taking over. Ends any gesture in progress.</summary>
    void Deactivate();
}
