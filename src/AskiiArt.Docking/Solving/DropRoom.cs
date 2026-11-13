// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;

namespace AskiiArt.Docking.Solving;

/// <summary>Where a window drag has the windows make room for its marker.</summary>
public enum DropRoomKind
{
    /// <summary>On a side with no windows, which opens by the room's thickness.</summary>
    EmptySide,

    /// <summary>A new column, before the one at <see cref="DropRoom.Column"/>, or past the last.</summary>
    Column,

    /// <summary>In a column's stack, before the group at <see cref="DropRoom.Position"/>, or past the last.</summary>
    Stack,
}

/// <summary>
/// Room the solver leaves empty for a dragged window's marker, so that the windows around it move apart to show where the
/// window would go (docs/DOCKING.md §5.2). It's an input to the solver, like a sub-toolbar, and never saved.
/// </summary>
/// <param name="Kind">What kind of room.</param>
/// <param name="Edge">The side it's on.</param>
/// <param name="Thickness">How wide it is on a side or as a column, or how tall in a stack.</param>
/// <param name="Column">For a new column, where it goes among the side's columns. For a stack, which column.</param>
/// <param name="Position">For a stack, where it goes among the column's groups.</param>
public sealed record DropRoom(DropRoomKind Kind, SideEdge Edge, double Thickness, int Column = 0, int Position = 0);
