// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Commands;

/// <summary>One cell's change inside a <see cref="CellEditCommand"/>.</summary>
/// <param name="Index">The cell's row-major index in the layer: <c>y * width + x</c>.</param>
/// <param name="Before">The cell before the change.</param>
/// <param name="After">The cell after the change.</param>
public readonly record struct CellEdit(int Index, Cell Before, Cell After);
