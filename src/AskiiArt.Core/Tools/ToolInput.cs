// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tools;

/// <summary>Modifier keys held during tool input, the same on every platform.</summary>
[Flags]
public enum ToolModifiers
{
    /// <summary>No modifier keys.</summary>
    None = 0,

    /// <summary>Ctrl on Windows and Linux, Cmd on macOS.</summary>
    Command = 1,

    /// <summary>Shift.</summary>
    Shift = 2,

    /// <summary>Alt on Windows and Linux, Option on macOS.</summary>
    Alt = 4
}

/// <summary>The keys a tool can respond to. Typed characters arrive separately, as text.</summary>
public enum ToolKey
{
    /// <summary>The left arrow key.</summary>
    Left,

    /// <summary>The right arrow key.</summary>
    Right,

    /// <summary>The up arrow key.</summary>
    Up,

    /// <summary>The down arrow key.</summary>
    Down,

    /// <summary>Home (Fn+Left on a Mac laptop).</summary>
    Home,

    /// <summary>End (Fn+Right on a Mac laptop).</summary>
    End,

    /// <summary>Backspace (Delete on a Mac keyboard).</summary>
    Backspace,

    /// <summary>Delete, which erases forwards (Fn+Delete on a Mac laptop).</summary>
    Delete,

    /// <summary>Enter or Return.</summary>
    Enter,

    /// <summary>Esc, which cancels.</summary>
    Escape
}

/// <summary>
/// A pointer event in canvas terms (ARCHITECTURE §8.1). The App translates the UI framework's
/// events into these, so tools never see pixels.
/// </summary>
/// <param name="Cell">The cell under the pointer. It may lie outside the canvas during a drag.</param>
/// <param name="SubCellX">Where across the cell the pointer is, from 0 (left edge) to 1 (right edge).</param>
/// <param name="SubCellY">Where down the cell the pointer is, from 0 (top edge) to 1 (bottom edge).</param>
/// <param name="Modifiers">The modifier keys held.</param>
/// <param name="Pressure">Pen pressure from 0 to 1; 1 for a mouse. Reserved for drawing tablets.</param>
public readonly record struct ToolPointerInput(
    CellPoint Cell,
    double SubCellX,
    double SubCellY,
    ToolModifiers Modifiers,
    float Pressure = 1);

/// <summary>
/// What the canvas draws on top of the document for the active tool. It isn't part of the
/// document. Brush and shape previews join it in later milestones.
/// </summary>
/// <param name="Caret">The text caret's cell, or null when there's no caret.</param>
public sealed record ToolOverlay(CellPoint? Caret)
{
    /// <summary>Nothing to draw.</summary>
    public static ToolOverlay None { get; } = new(Caret: null);
}
